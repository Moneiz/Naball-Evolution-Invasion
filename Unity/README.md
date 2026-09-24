# Naball sous Unity : tranche verticale Ger_FieldSwamp

Ce dossier est un projet Unity 6 qui reprend le hub du jeu, **Ger_FieldSwamp** (l'Île Cosmologique),
à partir du `.blend` original. La boule se déplace comme dans le BGE, les 20 Clims se ramassent et
sont sauvegardés, les 6 portails respectent l'avancement, les zones musicales et les dialogues fonctionnent.

![Le niveau exporté, rendu dans Blender avec les matériaux reconstruits](Docs/apercu_export_ger.png)

## Ouvrir le projet

1. Unity Hub › *Add project from disk* › choisir ce dossier `Unity/` (Unity 6000.0 LTS).
2. À la première ouverture, le script `LevelBuilder` construit tout seul `Assets/Naball/Scenes/Ger_FieldSwamp.unity`
   (matériaux, prefabs de la boule et du Clim, colliders, sons, portails). On peut le relancer via le menu
   **Naball › Construire Ger_FieldSwamp**.
3. Ouvrir la scène et appuyer sur Play. La boule tombe du ciel sur l'île, comme au premier passage dans l'original.

La console affiche la conversion d'axes Blender → Unity que le builder a vérifiée sur les objets du niveau.
Un avertissement à cet endroit signifie que les points d'apparition risquent d'être décalés.

## Commandes

| Touche | Action | Logic brick d'origine |
|---|---|---|
| ↑ / W | avancer | Motion `dloc -0.1` par tick |
| ← → / A D | tourner | Motion `drot ±0.0524` par tick |
| Maj | sprint (pouvoir *Move*) | Motion `dloc -0.25` |
| Espace | sauter (pouvoir *Jump*), puis planer 2 s (pouvoir *Fly*) | Motion `dloc z 0.16`, propriété `Flying` |
| Q / E | tourner la caméra | touches A / Z de `Ger.cam` |
| Entrée | dialogue suivant | DialogsEngine 1.05 |
| F5 / F6 | avancement du hub −1 / +1 (éditeur uniquement) | `alr` de `ger.gs` |
| F9 | repartir d'une sauvegarde vierge (éditeur uniquement) | |

## Ce qui est porté

| Original | Unity |
|---|---|
| Objet `Cube` et ses 44 logic bricks | `NaballController` (Rigidbody, vitesses converties à 60 ticks/s) |
| Actuator Camera de `Ger.cam` | `FollowCamera` (hauteur 5, distance 15 à 20, amortissement 0.031) |
| `ger.clim1..20` + `Clim_white` (Steering, message `Add` au HUD) | `ClimSpawner`, `ClimPickup` |
| `Ger.portal.*` (Collision + `alr > n`, écran Load_lvl) | `Portal` ; les niveaux pas encore portés affichent un message |
| `AudiViews.GerMod` | `SoundField` (volume = 1 − distance / rayon) |
| `DlgInvocation` + `Ressources/Dialog.py` | `DialogSystem`, `DialogTrigger`, textes de `Assets/lang` convertis en JSON |
| `Maps/Ger.py` + `gameInstance/Cont/ger.sii` | `LevelDirector` (apparition, caméra, dialogue d'accueil, lumière selon l'avancement) |
| Fichiers `.gs` relus avec `eval()`, toujours dans `save1/` | `GameState` : un JSON par emplacement dans `Application.persistentDataPath` |

## Pas encore porté

- Les autres niveaux, les menus, le HUD complet (vie, Atomiums, cages) et les cinématiques.
- Le tir (*Eta*), *Solidify*, les Clims violets et rouges, l'OVNI, les nénuphars et les bateaux.
- Les matériaux ne gardent que leur première texture : les mélanges de textures du terrain et le défilement
  de l'eau (`Uv_scroll.py`) sont à refaire, de même que le spot qui suivait la boule (remplacé par un soleil).
- Ger_FieldSwamp ne contient aucun ennemi, donc l'IA des Terioriams viendra avec un autre niveau.

## Régénérer les données depuis le `.blend`

Tout ce qui est dans `Assets/Naball/{Models,Textures,Audio,Data,Resources}` est produit par les scripts de `Tools/` :

```sh
sh Unity/Tools/export_all.sh Ger_FieldSwamp   # nécessite Blender 2.8+ (testé avec 4.0) et Python 3
```

1. `extract_logic.py` lit directement `Assets/MAP_DATA.scenes` (format 2.78) avec `blendfile.py`, un petit lecteur
   écrit pour l'occasion. Il récupère ce que Blender 2.8+ a oublié : les logic bricks et leurs liens, les propriétés
   de jeu, les textures des matériaux Blender Internal et les 34 scripts intégrés au `.blend`.
2. `export_level.py`, lancé dans Blender, exporte le niveau, la boule et le Clim en FBX, avec les axes attendus par Unity.
3. `build_unity_data.py` copie modèles, textures et sons, puis écrit la description du niveau (rôle de chaque objet)
   et convertit les fichiers `.lg` en JSON.

Pour porter un autre niveau, il suffit de relancer la même chaîne avec son nom de scène, puis d'étendre `build_unity_data.py`
et `LevelBuilder` aux objets qu'il est seul à utiliser.
