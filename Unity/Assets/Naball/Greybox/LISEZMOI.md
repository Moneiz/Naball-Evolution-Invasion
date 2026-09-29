# Salles greybox

Chaque fichier `.json` de ce dossier décrit une salle jouable en blocs gris. Le menu
**Naball › Construire les salles greybox** en fait une scène dans `Assets/Naball/Scenes/Greybox/`,
avec Lumka, la caméra, le HUD et le système de dimensions. On joue, on corrige le texte, on reconstruit.
F9 recommence la salle.

Sans Unity, `blender -b --python Unity/Tools/greybox_preview.py -- <salle>.json <image.png>` rend la salle
dans les trois dimensions côte à côte.

## Repères

- Mètres. x vers la droite, y vers le haut, z vers l'avant : le joueur part vers +z.
- La grille des blocs fait 2 m. Lumka saute environ 2,5 m de haut et 8 m de long en courant.
- Dimensions : `bleu` (fige, refroidit), `neutre`, `rouge` (accélère, réchauffe).

## Champs

| Champ | Contenu |
|---|---|
| `titre` | message affiché au départ |
| `apparition`, `orientation` | position des pieds de Lumka, et direction en degrés (0 = +z) |
| `dimensionDepart`, `dureeBascule` | dimension au lancement, durée d'une bascule en secondes |
| `pouvoirs` | pouvoirs donnés dès le départ : `bascule`, `tir`, `dash` |
| `pieces` | le décor, voir ci-dessous |
| `fruits` | `position`, `vers` : le fruit fait basculer vers cette dimension, et repousse |
| `clims` | `position`, `dimensions` (vide = partout) |
| `atomiums` | `position`, `pouvoir`, `message` : donne le pouvoir au contact |
| `controles` | points de contrôle : on y réapparaît après une chute |
| `panneaux` | `position`, `texte` : consignes flottantes |
| `fin` | `position`, `texte` : fin de la salle |

### Pièces

```json
{ "nom": "Pont", "forme": "bloc", "position": [0, 0.4, 25], "taille": [3, 0.6, 10],
  "dimensions": "bleu,neutre", "rouge": [0, 6, 0], "rotationNeutre": [0, 90, 0], "surface": "sol" }
```

- `forme` : `bloc`, `rampe` (monte vers +z), `pilier`.
- `position` : **centre de la face du dessous**. Un sol de hauteur 1 posé à y = 0 a son dessus à y = 1.
- `taille` : largeur (x), hauteur (y), profondeur (z). `rotation` : degrés.
- `dimensions` : où la pièce existe (vide = partout). Ailleurs, elle n'est plus qu'un écho translucide
  sans collision.
- `bleu`, `neutre`, `rouge` : décalage de position dans cette dimension ; `rotationBleu`, `rotationNeutre`,
  `rotationRouge` : rotation ajoutée. Pendant la bascule, la pièce glisse d'une position à l'autre et porte Lumka.
- `surface` : `sol`, `mur` (plus sombre) ou `lave` (renvoie au dernier point de contrôle).

Couleurs dans Unity : gris pour ce qui existe partout, bleuté ou rougi pour ce qui n'existe que d'un côté,
orange lumineux pour la lave.
