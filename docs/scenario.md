# Scénario et plan des niveaux — proposition

Proposition du 29/09/2026 pour le remake avec Lumka. Décidé par Alan : Naball disparaît complètement,
la 4D est la boucle de gameplay principale, introduite dès le début, et le vol, le double saut et Solidify
sont supprimés. Elle garde tout ce que l'original
a posé (Opus Climus, Arial, Clims, Terioriams, Helpi, la dalle magique, les pouvoirs) et y branche les idées
du prototype lumka-player (fruits 4D, dimensions, Somtraj, « les échos du chaos »).

## 1. Ce que dit l'original, et où il n'est pas cohérent

Relu dans le `.blend` (logic bricks, scènes chaînées), `Assets/lang/fr` et le prologue intégré.

**Ce qui tient bien** : le prologue (l'Opus Climus, Arial à l'énergie infinie mais instable, l'invasion
des Terioriams, l'explosion qui disperse les Clims), Helpi qui parle en pensée, la dalle magique « reliée
aux dimensions » qui donne les pouvoirs grâce aux Atomiums, le hub qui ouvre les mondes un par un.

**Les incohérences à régler** :

| # | Constat | Où |
|---|---|---|
| 1 | Le hub annonce Prairie → « marais rocheux » → « Cité des Terioriams » → « désert rocheux », mais ses portails mènent à Prairie (avancement 0), Swamp_bug (2), Underground (3), Rock_desert (4). La « Cité » est donc Underground, un niveau généré par script (`WorldGenerator`), pas une cité. | dialogues `#02x02`–`#02x04`, portails de Ger |
| 2 | Trois scènes ne sont atteintes par aucun portail : Swamp_intro (lancée depuis le menu, mène à Underground), Mountain_high (mène à Prairie et au hub) et Desert_lava (appelée par un script du hub). Elles n'ont que 4 à 7 Clims de test : ce sont des niveaux commencés. | chaînage des scènes |
| 3 | Deux portails du hub sont scellés pour toujours : un second vers Swamp_bug et un vers la Prison. | portails `locked` |
| 4 | Le compte des Clims ne tombe pas juste : le prologue parle de 5 000 Clims, Helpi de 161, les statistiques calculent sur 92. Les niveaux posent environ 98 Clims bleus numérotés (Ger 20, Prairie 30, Swamp_bug 20, Rock_desert 15, Underground 8, Prison 5), plus 70 promis par les cages, soit ~168. | `#01x02`, `SaveBloc.statsUpdate` |
| 5 | Helpi annonce 7 Arialiens en cage, les niveaux en contiennent 8 (Prairie 3, Prison 2, Rock_desert 2, Underground 1). | `#01x03` |
| 6 | Le jeu commence dans la Prison des Terioriams (cinématique, puis la dalle et le pouvoir Alpha), et la même scène sert aussi à la fin (« And Then and Then » → Chariot_mine2 → Final_game). La Prison est à la fois le premier et le dernier niveau, sans que l'histoire explique le retour. | `Animation_ennemie1`, `Prison of the Terioriams` |
| 7 | La fin avoue que « l'univers de Naball n'est pas terminé » et renvoie vers un niveau bonus : il n'y a pas de dénouement. | `final` |
| 8 | Le vol vient d'un Clim violet (2 s), alors que le remake en fait un double saut permanent. | `#06x01`, `LumkaController` |
| 10 | La dalle est « reliée aux dimensions », mais aucune mécanique de l'original n'exploite les dimensions. | `#01x01` |
| 9 | Tous les dialogues s'adressent à Naball, et les commandes citées (flèches, touche C, A/Z pour la caméra) ne sont plus les bonnes. | `Assets/lang` |

## 2. La boucle de jeu : la 4D

Tout le jeu tourne autour d'une idée : **le monde existe en plusieurs dimensions superposées, et Lumka
passe de l'une à l'autre**. Le prototype l'avait posée avec les fruits 4D : une valeur de dimension de −10
(bleu) à +10 (rouge), et des objets dont la position, la rotation et la visibilité suivent une courbe
selon cette valeur.

**La boucle, à chaque salle** :
1. **Observer** : la teinte de l'écran et un écho visuel (silhouettes translucides) montrent ce qui
   existe dans les autres dimensions.
2. **Basculer** : Lumka change de dimension (fruit 4D, puis à volonté quand le pouvoir est acquis).
3. **Le monde se recompose** : ponts qui s'alignent, plateformes qui montent, murs qui disparaissent,
   eau qui devient glace, lave qui devient roche, ennemis qui apparaissent ou s'évanouissent.
4. **Traverser**, souvent en rebasculant en plein saut ou pendant la recomposition.
5. **Récompense** : les Clims bleus les mieux cachés n'existent que dans une dimension.

**Ce qui rend la mécanique riche au lieu d'être un simple interrupteur** :
- **La bascule prend du temps** (environ une demi-seconde) : la dimension glisse de sa valeur actuelle
  à la nouvelle, et les objets suivent leur courbe. Une plateforme qui monte pendant la bascule porte Lumka ;
  un mur qui se referme peut l'écraser. Ça crée des énigmes de timing, pas seulement de logique.
- **Les dimensions ont chacune leur règle** : le bleu (−) fige et refroidit (eau gelée, lave en roche,
  ennemis ralentis) ; le rouge (+) accélère et réchauffe (courants, geysers, plantes qui poussent) ;
  le neutre (0) est le monde normal. Chaque niveau exploite ces règles avec ses propres objets.
- **Les ennemis vivent dans une dimension** : un Somtraj rouge ne voit pas Lumka en bleu ; un Terioriam
  « ancré » reste présent partout et doit être vaincu. Le combat devient du placement entre dimensions.
- **L'énergie** (la jauge du dash et du tir) paie aussi les bascules libres, ce qui empêche de basculer
  sans arrêt et relie les trois actions.

**Progression de la mécanique** :

| Étape | Où | Ce que Lumka peut faire |
|---|---|---|
| 1 | Prologue (5 premières minutes) | Un fruit 4D bleu fixe : le manger fait basculer, les barreaux de la cellule n'existent qu'en neutre |
| 2 | Prologue et Prairie | Fruits rouges et bleus posés dans le niveau ; chaque fruit impose sa dimension |
| 3 | Fin de la Prairie | **Bascule libre** entre neutre et une dimension (bouton dédié), au prix d'énergie |
| 4 | Marais rocheux | Bascule libre entre les trois : bleu, neutre, rouge |
| 5 | Monts Célestes | **Bascule en l'air** sans coût supplémentaire, enchaînée au dash et au Lien |
| 6 | Cité des Terioriams et Mines | **Ancre** puis **Eta de phase** : agir sur la dimension d'un seul objet |
| 7 | Déserts | Dimensions extrêmes (−10 / +10) : effets plus forts, énergie plus chère |
| 8 | Fin du jeu | Salles où la dimension change seule avec le temps, et boss qui bascule aussi |

## 3. Le nouveau scénario

**Titre de travail** : *Lumka — Les Échos du Chaos*.

**Prologue** (repris presque mot pour mot) : l'Opus Climus raconte Arial, planète à l'énergie infinie
mais instable. L'Empire des Terioriams l'attaque ; leur « tension sombre » fait exploser l'énergie
d'Arial, qui se disperse en Clims à travers l'Univers. Les Arialiens tombent aux mains de l'Empire.

**Ce que le remake ajoute** : l'explosion n'a pas seulement dispersé les Clims, elle a **fendu Arial entre
les dimensions**. Chaque monde du jeu est un *écho* d'Arial dans une autre dimension (une prairie, un marais,
un désert, une montagne), déformé par le chaos. Les fruits 4D poussent là où la fracture est la plus fine :
ce sont des morceaux d'Arial qui ont gardé la mémoire de toutes ses dimensions. C'est ce que dit le titre,
et c'est pourquoi la mécanique est au centre du récit.

**Lumka** est une jeune gardienne d'Arial, la seule restée libre. Elle se réveille en cellule dans la
Prison des Terioriams. En mangeant un fruit 4D tombé par une fissure, elle découvre qu'elle peut passer
d'une dimension à l'autre, ce que les Terioriams ne savent pas faire : c'est son avantage sur l'Empire.
Helpi, un Arialien prisonnier, lui parle en pensée et la guide.

**L'Empire des Terioriams** se nourrit du chaos : plus Arial reste fracturée, plus il est puissant.
Ses soldats sont les Terioriams (déjà modélisés), ses tourelles les Somtraj (prototype), ses vaisseaux
ceux de Rock_desert. Son chef, l'**Empereur Terioriam** (nom à trouver), veut garder les dimensions
séparées pour régner sur tous les échos à la fois. Il cherche à capturer Lumka pour s'emparer de son don.

**Le but** : ramasser les Clims bleus pour rendre sa lumière à Arial, libérer les Arialiens et réunir les
Atomiums, qui rendent à Lumka les pouvoirs des gardiens. Chaque monde terminé « recoud » un écho à Arial :
le hub change (lumière, végétation, Arialiens libérés qui s'y installent), comme l'éclairage de Ger qui
variait déjà selon l'avancement dans l'original.

**La fin** : Lumka rentre sur Arial, affronte l'Empereur dans sa citadelle à la jonction des dimensions,
où le combat se joue en basculant entre elles, puis utilise tous les Clims réunis pour refermer la fracture.
Arial retrouve sa lumière. Le titre du prototype, *Light of Arial*, peut être celui de ce dernier chapitre.

**Les objets, rendus cohérents** :
- **Clims bleus** : 200 au total (chiffre rond et affiché partout), dont 70 donnés par 7 Arialiens.
  Le prologue parle des Clims d'Arial en général, sans chiffre. Environ un tiers n'existe que dans une dimension.
- **Clims rouges** : vie. **Clims blancs** : mécanismes des Terioriams, parfois piégés.
- **Fruits 4D** : imposent une dimension ; plus tard, rechargent l'énergie de bascule.

## 4. Pouvoirs, dans l'ordre où Lumka les gagne

Pas de vol, pas de double saut, pas de Solidify. Le déplacement de base reste simple (course, saut à hauteur
variable, dash, accroche aux rebords) et **chaque pouvoir gagné ensuite agit sur les dimensions** : il donne
une nouvelle façon d'utiliser la bascule plutôt qu'une nouvelle façon de sauter plus haut.

**Début de jeu** (prologue à chapitre 2) :

| Pouvoir | Chapitre | Effet |
|---|---|---|
| Alpha : courir | Prologue | course analogique |
| Altopy : sauter | Prologue | saut à hauteur variable, accroche aux rebords |
| Fruits 4D | Prologue | la dimension change en mangeant un fruit |
| Bascule libre | 1. Prairie | changer de dimension à volonté, contre de l'énergie |
| Dash | 1. Prairie | au sol, puis une fois en l'air |
| Eta : tir | 2. Marais | tir, cages, cibles |

**Milieu de jeu** (chapitres 3 à 8), un pouvoir par chapitre :

| Pouvoir | Chapitre | Effet | Ce qu'il ouvre comme situations |
|---|---|---|---|
| **Lien** | 3. Monts Célestes | Un fil de lumière qui tire Lumka vers un point d'accroche (fleurs d'Arial). Les points n'existent que dans une dimension. | Traversées verticales : basculer en plein saut pour faire apparaître le point suivant, puis s'y lier. Remplace le vol pour franchir les grands vides. |
| **Ancre** | 4. Cité des Terioriams | Poser une ancre sur un objet : il garde sa dimension quand Lumka bascule (3 ancres au plus). | Garder un pont bleu en rouge ; bloquer une porte ouverte ; empêcher une patrouille de basculer avec le monde. |
| **Eta de phase** | 5. Mines | Le tir Eta envoie la cible dans l'autre dimension, sans faire basculer le reste. | Désarmer un Somtraj en l'exilant ; faire apparaître une seule plateforme ; renvoyer les bombes des vaisseaux dans une dimension où elles ne touchent rien. |
| **Écho** | 6. Désert rocheux | Laisser une copie immobile de Lumka dans la dimension actuelle. Elle reste sur une dalle, sert d'appât ou de cible pour le Lien. | Énigmes à deux corps : l'écho tient la dalle en bleu pendant que Lumka avance en rouge. Leurres pour les patrouilles. |
| **Bulle** | 7. Désert de lave | Une sphère d'environ 5 m, centrée sur Lumka, où règne l'autre dimension pendant quelques secondes. | La lave reste roche seulement dans la bulle : avancer vite sans la perdre. Traverser un mur en ne basculant que ce qui est autour de soi. |
| **Déphasage** | 8. Forêt des échos | Lumka seule passe entre deux dimensions une seconde : intouchable, elle traverse ce qui n'existe que d'un côté. | Esquiver les attaques et les vagues de la Forêt ; franchir une grille au dernier moment ; clé du combat contre l'Empereur. |

**Fin de jeu** (chapitres 9 à 11) : pas de nouveau pouvoir. Les salles demandent de les combiner
(Écho posé sur une dalle, Ancre sur un pont, Bulle pour traverser, Lien pour sortir), et l'Empereur
les utilise à son tour.

**Règles communes** : chaque pouvoir coûte de l'énergie, la même jauge que la bascule, le dash et le tir.
Les Atomiums restent la façon de les obtenir : la dalle magique du hub les révèle, comme dans l'original.
Chaque pouvoir est enseigné dans une salle sûre de son chapitre, puis testé dans un défi du hub
qui rapporte des Clims.

## 5. Plan des niveaux pour 10 à 15 heures

Durées estimées pour un joueur qui ramasse une bonne partie des Clims ; ce sont des ordres de grandeur.
« Existant » veut dire que la scène est dans le `.blend` et se porte avec la chaîne actuelle ; « Nouveau »
demande de la modélisation. Chaque niveau existant reçoit une couche 4D : des objets ajoutés ou dédoublés
dans le builder, sans remodéliser le décor.

| # | Chapitre | Scène | Nature | Ce que la 4D y apporte | Durée |
|---|---|---|---|---|---|
| 0 | Prologue : la Prison | Prison of the Terioriams (1re moitié) | Existant, à scinder | Premier fruit, évasion par les barreaux absents en neutre, Alpha et Altopy | 25 min |
| — | Hub : l'Île Cosmologique | Ger_FieldSwamp | Existant, porté | Portails visibles seulement dans certaines dimensions ; l'île se recompose à chaque monde recousu | 1 h au total |
| 1 | La Prairie des eaux | Prairie_of_the waters | Existant | Ruisseaux gelés en bleu, courants en rouge ; bascule libre et dash | 1 h |
| 2 | Le Marais rocheux | Swamp_intro + Swamp_bug | Existant ×2, à relier | Nénuphars qui poussent en rouge ; trois dimensions ; Eta ; premier boss (un Terioriam ancré) | 1 h 15 |
| 3 | Les Monts Célestes | Mountain_high | Existant, à terminer | Météo selon la dimension, bascule en l'air, Lien | 1 h |
| 4 | La Cité des Terioriams | Underground | Existant, à refaire à la main | Infiltration : patrouilles qui ne voient qu'une dimension ; Ancre ; Somtraj | 1 h 15 |
| 5 | Les Mines | Chariot_mine + Chariot_mine2 | Existant | Fuite en wagonnet : basculer pour faire apparaître les rails ; Eta de phase | 30 min |
| 6 | Le Désert rocheux | Rock_desert | Existant | Écho, mode combat, vaisseaux dont on renvoie les bombes | 1 h |
| 7 | Le Désert de lave | Desert_lava | Existant, à terminer | Dimensions extrêmes, lave et roche en alternance ; Bulle | 1 h |
| 8 | La Forêt des échos | *nouveau* | Nouveau | La dimension change seule par vagues ; énigmes de timing ; Déphasage | 1 h |
| 9 | Retour à la Prison | Prison of the Terioriams (2e moitié) | Existant, à scinder | La prison revisitée avec tous les pouvoirs : libérer les derniers Arialiens | 50 min |
| 10 | Arial en ruines | *nouveau* (île, maisons, pont du prototype) | Nouveau, base existante | Les dimensions presque recousues se superposent | 1 h |
| 11 | La Citadelle impériale | *nouveau* | Nouveau | Ascension, puis l'Empereur qui bascule avec Lumka | 1 h |
| — | Épilogue | Final_game, « And Then and Then » | Existant | Arial rallumée, crédits | 10 min |

**Total** : environ 12 h en ligne droite, 15 h en cherchant tous les Clims. 10 chapitres sur 13 s'appuient
sur des scènes déjà dans le `.blend`.

**Répartition des 200 Clims** (proposition) : Ger 20, Prairie 20, Marais 20, Monts 12, Cité 12, Mines 6,
Désert rocheux 12, Désert de lave 10, Forêt des échos 10, Prison 5, Arial 3, soit 130 posés, plus 70
donnés par les 7 Arialiens.

**Portails du hub** : un portail par chapitre (1 à 8), ouverts dans l'ordre, certains visibles seulement
dans une dimension. Les chapitres 9 à 11 passent par le portail scellé de la Prison, qui ne s'ouvre qu'après
la Forêt des échos : c'est ce qui explique le retour en Prison (incohérence 6) et donne un rôle au portail
scellé (incohérence 3).

## 6. Ce que ça change pour le remake

- **Priorité n° 1 : le système de dimensions**, avant de porter d'autres niveaux. Reprendre `TransDimensional*`
  du prototype en le remettant d'aplomb : bascule progressive au lieu d'instantanée, objets qui portent
  Lumka pendant qu'ils bougent, dimension par objet (ancres), écho visuel des autres dimensions, teinte
  d'écran sans passer par URP. Puis une salle de test dans le hub pour régler les sensations.
- Retirer Naball : la boule, son menu de construction et les dialogues qui la nomment.
- Réécrire les dialogues de `Assets/lang` pour Lumka, la 4D et les nouvelles commandes.
- Retirer le double saut de `LumkaController` et les Clims violets ; ajouter l'accroche aux rebords.
- Pouvoirs de milieu de jeu à prototyper dans la salle de test, dans l'ordre : Ancre, Eta de phase, Bulle, Écho, Lien, Déphasage.
- Scinder la Prison en prologue et chapitre 9 ; relier Swamp_intro et Swamp_bug ; terminer Mountain_high et Desert_lava.
- Refaire Underground à la main en cité (son `WorldGenerator` n'a pas d'équivalent Unity).
- Nouveaux besoins de modélisation : la Forêt des échos, Arial en ruines (partir de la scène `Test_arial`
  du prototype), la Citadelle, l'Empereur.

## Décisions attendues d'Alan

1. La liste des pouvoirs de milieu de jeu (section 4) : lesquels garder, lesquels remplacer ?
