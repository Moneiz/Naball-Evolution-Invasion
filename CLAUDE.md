# Naball Evolution Invasion

Jeu de plateforme 3D d'Alan (GitHub `Moneiz`), écrit entre 13 et 17 ans avec le **Blender Game Engine 2.77**.
Le BGE n'existe plus : le jeu est en cours de **remake sous Unity 6**, niveau par niveau, avec **Lumka**
(personnage du prototype Unity [lumka-player](https://gitlab.com/piwel-lumka/lumka-player), 2021) comme héros.
On réutilise les niveaux modélisés pour Naball.

On échange en français avec Alan. Commentaires de code, textes du jeu, commits et descriptions de PR en français.

## Le jeu

- Le hub **Ger_FieldSwamp** (l'Île Cosmologique) mène aux niveaux par des portails qui s'ouvrent selon l'avancement
  (`alr` dans les sauvegardes `.gs`, `progress` côté Unity).
- On ramasse des **Clims** (161 au total) ; les pouvoirs du héros se débloquent au fil du jeu :
  *Move*, *Jump*, *Fly*, *Eta* (tir), *Solidify*.
- Scènes du `.blend` : Ger_FieldSwamp, Swamp_intro, Swamp_bug, Prairie_of_the waters, Rock_desert, Desert_lava,
  Mountain_high, Underground, Prison of the Terioriams, Cart_world, Chariot_mine, Chariot_mine2, Final_game,
  plus menus, HUD, chargement et cinématiques.
- Ennemis : les Terioriams (absents du hub). Côté Lumka : le Somtraj (tourelle) et les fruits 4D du prototype.

## Organisation du dépôt

| Chemin | Contenu |
|---|---|
| `Assets/MAP_DATA.scenes` | Le `.blend` 2.78 de tout le jeu (24 Mo) : scènes, logic bricks, 34 scripts intégrés |
| `Assets/scripts/` | Scripts Python 3.5 du BGE (référence du comportement, **ne plus les corriger**) |
| `Assets/lang/` | Textes et dialogues `.lg` (dictionnaires Python), FR et EN |
| `Assets/gameInstance/` | État initial des niveaux (`.sii`) |
| `Naball.exe`, `*.dll`, `2.77/` | Runtime Windows du jeu original |
| `Unity/` | Le remake Unity 6 (6000.0.23f1). Voir `Unity/README.md` |
| `Unity/Tools/` | Chaîne d'extraction `.blend` → Unity (Python + Blender) |
| `Unity/Assets/Naball/` | Scripts, données et modèles générés du remake |
| `Unity/Assets/Lumka/` | Modèle, animations, textures et sons de Lumka, repris du prototype avec leurs `.meta` |

## Remake Unity : règles

- Porter un niveau : `sh Unity/Tools/export_all.sh <Scène>` (Blender 2.8+ sur le PATH, testé avec 4.0 + `python3-numpy`),
  puis étendre `build_unity_data.py` et `LevelBuilder` aux objets propres à ce niveau.
- `Unity/Tools/blendfile.py` lit directement le format 2.78 : Blender 4 perd les logic bricks et les propriétés de jeu.
  Le comportement d'un objet se lit dans ses logic bricks (`extract_logic.py`) et les scripts de `Assets/scripts`.
- Les scènes Unity, prefabs et matériaux sont **générés** par les scripts `Editor/` (`LevelBuilder`, `LumkaBuilder`,
  `MaterialBuilder`) : on modifie le code des builders, pas les assets générés. Rien n'est construit à la main dans l'éditeur.
- Une classe `MonoBehaviour` par fichier, nom du fichier = nom de la classe, namespace `Naball` (`Naball.EditorTools` pour l'éditeur).
- Pipeline de rendu intégré (Standard). Pas d'URP tant qu'un besoin concret (post-process des dimensions) ne le justifie pas.
- Le personnage joué dérive de `PlayerCharacter` ; Clims, portails et dialogues ne connaissent que cette classe.
- Commandes via `Controls` : clavier AZERTY et QWERTY, manette. Vitesses de la boule converties depuis les ticks BGE (60/s).

## Vérifier sans Unity

L'éditeur Unity ne tourne pas dans les conteneurs cloud. On vérifie :
- le C# avec `dotnet build` contre les assemblies Unity 2021 (NuGet `unity3d.sdk`) et de petits stubs UI ;
  seules les API propres à Unity 6 (`linearVelocity`, `linearDamping`, `angularDamping`, `FindAnyObjectByType`,
  `PhysicsMaterial`) doivent échouer ;
- les exports FBX en les réimportant dans Blender (rendu Cycles sans débruitage).

Toujours dire à Alan ce qui n'a pas pu être testé en jeu.

## Git

- Pas de CI sur le dépôt. Branche par défaut : `master`.
- PR en français, au format « Before / After / How ».
- Ne pas committer `__pycache__/` ni les dossiers générés par Unity (`Library/`, `Temp/`, …).

## Skills du projet

- `.claude/skills/product-owner` : transformer une idée en besoin clair, backlog priorisé, critères d'acceptation.
- `.claude/skills/chef-de-projet` : feuille de route, point d'avancement, risques et prochaines étapes.
