# Lecteur minimal du format .blend (64 bits, little endian), sans dépendance à Blender.
# Sert à relire ce que Blender 2.8+ ne sait plus ouvrir : logic bricks, propriétés de jeu,
# textures des matériaux Blender Internal.
import re
import struct

_FMT = {"char": "b", "uchar": "B", "short": "h", "ushort": "H", "int": "i", "float": "f",
        "double": "d", "int64_t": "q", "uint64_t": "Q"}


class Blend:
    def __init__(self, path):
        f = self.f = open(path, "rb").read()
        if f[:7] != b"BLENDER" or f[7:9] != b"-v":
            raise ValueError("Seuls les .blend 64 bits little endian sont gérés")
        self.version = f[9:12].decode()
        self.blocks, self.byptr = [], {}
        p = 12
        while p < len(f):
            code = f[p:p + 4].rstrip(b"\0").decode("latin1")
            size, = struct.unpack("<i", f[p + 4:p + 8])
            ptr, = struct.unpack("<Q", f[p + 8:p + 16])
            sdna, count = struct.unpack("<ii", f[p + 16:p + 24])
            block = (code, size, ptr, sdna, count, p + 24)
            self.blocks.append(block)
            if ptr:
                self.byptr[ptr] = block
            p += 24 + size
            if code == "ENDB":
                break
        self._read_dna()

    def _read_dna(self):
        dna = next(b for b in self.blocks if b[0] == "DNA1")
        d = self.f[dna[5]:dna[5] + dna[1]]

        def strings(q):
            n, = struct.unpack("<i", d[q:q + 4])
            q += 4
            out = []
            for _ in range(n):
                e = d.index(b"\0", q)
                out.append(d[q:e].decode())
                q = e + 1
            return out, (q + 3) & ~3

        names, q = strings(8)
        types, q = strings(q + 4)
        lengths = struct.unpack("<%dh" % len(types), d[q + 4:q + 4 + 2 * len(types)])
        q = (q + 4 + 2 * len(types) + 3) & ~3
        count, = struct.unpack("<i", d[q + 4:q + 8])
        q += 8
        self.structs, self.index = [], {}
        for i in range(count):
            t, nf = struct.unpack("<hh", d[q:q + 4])
            q += 4
            fields, off = {}, 0
            for j in range(nf):
                ft, fn = struct.unpack("<hh", d[q + 4 * j:q + 4 * j + 4])
                name = names[fn]
                size = 8 if name.startswith(("*", "(*")) else lengths[ft]
                mult = 1
                for dim in re.findall(r"\[(\d+)\]", name):
                    mult *= int(dim)
                base = re.sub(r"\[.*", "", name).strip("*()")
                fields.setdefault(base, (off, types[ft], name, size, mult))
                off += size * mult
            q += 4 * nf
            self.structs.append((types[t], fields))
            self.index[types[t]] = i

    def fields(self, sname):
        return self.structs[self.index[sname]][1]

    def get(self, sname, off, field):
        o, t, name, size, mult = self.fields(sname)[field]
        a = off + o
        if name.startswith("*"):
            return struct.unpack("<Q", self.f[a:a + 8])[0]
        if t == "char" and mult > 1:
            return self.f[a:a + mult].split(b"\0")[0].decode("utf-8", "replace")
        fmt = _FMT.get(t)
        if fmt is None:
            return a  # structure imbriquée : on renvoie son adresse
        if mult > 1:
            return list(struct.unpack("<%d%s" % (mult, fmt), self.f[a:a + size * mult]))
        return struct.unpack("<" + fmt, self.f[a:a + size])[0]

    def ptr_array(self, sname, off, field):
        o, _, _, _, mult = self.fields(sname)[field]
        return [struct.unpack("<Q", self.f[off + o + 8 * i:off + o + 8 * i + 8])[0] for i in range(mult)]

    def block(self, ptr):
        return self.byptr.get(ptr) if ptr else None

    def cstring(self, ptr):
        b = self.block(ptr)
        return self.f[b[5]:b[5] + b[1]].split(b"\0")[0].decode("utf-8", "replace") if b else ""

    def listbase(self, sname, off, field):
        a = self.get(sname, off, field)
        cur, = struct.unpack("<Q", self.f[a:a + 8])
        out = []
        while cur in self.byptr:
            b = self.byptr[cur]
            out.append(b)
            cur, = struct.unpack("<Q", self.f[b[5]:b[5] + 8])
        return out

    def id_name(self, off):
        return self.get("ID", off, "name")[2:]
