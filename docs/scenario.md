# Scénario et plan des niveaux — proposition

Proposition du 29/09/2026 pour le remake avec Lumka, à valider par Alan. Elle garde tout ce que l'original
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
| 9 | Tous les dialogues s'adressent à Naball, et les commandes citées (flèches, touche C, A/Z pour la caméra) ne sont plus les bonnes. | `Assets/lang` |

## 2. Le nouveau scénario

**Titre de travail** : *Lumka — Les Échos du Chaos*.

**Prologue** (repris presque mot pour mot) : l'Opus Climus raconte Arial, planète à l'énergie infinie
mais instable. L'Empire des Terioriams l'attaque ; leur « tension sombre » fait exploser l'énergie
d'Arial, qui se disperse en Clims à travers l'Univers. Les Arialiens tombent aux mains de l'Empire.

**Ce que le remake ajoute** : l'explosion n'a pas seulement dispersé les Clims, elle a **fendu Arial entre
les dimensions**. Chaque monde du jeu est un *écho* d'Arial dans une autre dimension (une prairie, un marais,
un désert, une montagne), déformé par le chaos. C'est ce qui relie la dalle « reliée aux dimensions » de
l'original aux fruits 4D du prototype, et c'est ce que dit le titre.

**Lumka** est une jeune gardienne d'Arial. Les gardiens protégeaient l'énergie de la planète ; Lumka est la
seule qui reste libre. Elle se réveille en cellule dans la Prison des Terioriams, sans pouvoirs, et Helpi
lui parle en pensée.

**Et Naball ?** Deux options, à choisir :
- **A (recommandée)** : Naball était l'ancien gardien d'Arial, un esprit en forme de sphère, dissous dans
  l'explosion. Ses fragments sont les Atomiums ; chaque pouvoir que Lumka gagne est un morceau de Naball.
  « Naball Evolution » prend un sens : Lumka est ce que Naball devient. La boule peut réapparaître en
  souvenir ou en esprit dans les cinématiques.
- **B** : Naball disparaît du récit et ne reste que dans le titre de la série.

**L'Empire des Terioriams** se nourrit du chaos : plus Arial reste fracturée, plus il est puissant.
Ses soldats sont les Terioriams (déjà modélisés), ses tourelles les Somtraj (prototype), ses vaisseaux
ceux de Rock_desert. Il lui faut un chef, l'**Empereur Terioriam** (nom à trouver), qui veut garder les
dimensions séparées pour régner sur tous les échos à la fois.

**Le but** : ramasser les Clims bleus pour rendre sa lumière à Arial, libérer les Arialiens et réunir les
Atomiums pour recomposer les pouvoirs des gardiens. Chaque monde terminé « recoud » un écho à Arial : le hub
change (lumière, végétation, Arialiens libérés qui s'y installent), ce que l'original faisait déjà avec
l'éclairage de Ger selon l'avancement.

**La fin** : Lumka rentre sur Arial, affronte l'Empereur dans sa citadelle à la jonction des dimensions,
puis utilise tous les Clims réunis pour refermer la fracture. Arial retrouve sa lumière. Le titre du
prototype, *Light of Arial*, peut être celui de ce dernier chapitre.

**Les objets, rendus cohérents** :
- **Clims bleus** : 200 au total (chiffre rond et affiché partout), dont 70 donnés par 7 Arialiens.
  Le prologue parle des Clims d'Arial en général, sans chiffre.
- **Clims rouges** : vie. **Clims blancs** : mécanismes des Terioriams, parfois piégés.
- **Clims violets** : planer quelques secondes, comme dans l'original. Le double saut devient un pouvoir
  gagné plus tard, pour que le Clim violet garde son utilité.
- **Fruits 4D** : passer d'une dimension à l'autre dans un niveau (plateformes qui apparaissent, ponts qui
  s'alignent). Ils arrivent au milieu du jeu, une fois les bases maîtrisées.
- **Énergie** (dash, tir) : se recharge seule, et plus vite en ramassant des Clims.

## 3. Pouvoirs, dans l'ordre où Lumka les gagne

| Pouvoir | Nom d'origine | Où | Effet |
|---|---|---|---|
| Se déplacer, courir | Alpha | Prison (prologue) | course analogique |
| Sauter | Altopy | Prison (prologue) | saut à hauteur variable |
| Dash | *nouveau* | Prairie des eaux | dash au sol, puis en l'air |
| Tir | Eta | Marais rocheux | tir, cages, cibles, renvoi des bombes |
| Double saut | *nouveau* (ex-Fly) | Monts Célestes | salto en l'air |
| Fruits 4D | *prototype* | Cité des Terioriams | changement de dimension |
| Solidifier | Solidify | Désert rocheux | changer la lave en roche |
| Mode combat | *prototype* | Désert rocheux | verrouillage sur un ennemi (Ctrl) |

## 4. Plan des niveaux pour 10 à 15 heures

Durées estimées pour un joueur qui ramasse une bonne partie des Clims ; ce sont des ordres de grandeur.
« Existant » veut dire que la scène est dans le `.blend` et se porte avec la chaîne actuelle ; « Nouveau »
demande de la modélisation.

| # | Chapitre | Scène | Nature | Contenu | Durée |
|---|---|---|---|---|---|
| 0 | Prologue : la Prison | Prison of the Terioriams (1re moitié) | Existant, à scinder | Cinématique Opus Climus, réveil, dalle, Alpha et Altopy, évasion | 20 min |
| — | Hub : l'Île Cosmologique | Ger_FieldSwamp | Existant, porté | Retour entre chaque monde, change avec l'avancement | 1 h au total |
| 1 | La Prairie des eaux | Prairie_of_the waters | Existant | Premiers Terioriams, 3 cages, dash | 50 min |
| 2 | Le Marais rocheux | Swamp_intro + Swamp_bug | Existant ×2, à relier | Nénuphars et graines, Eta, premier boss | 1 h 15 |
| 3 | Les Monts Célestes | Mountain_high | Existant, à terminer | Météo, sous l'eau, Clims violets, double saut | 1 h |
| 4 | La Cité des Terioriams | Underground | Existant, à refaire à la main | Infiltration, fruits 4D, Somtraj | 1 h 15 |
| 5 | Les Mines | Chariot_mine + Chariot_mine2 | Existant | Fuite de la Cité en wagonnet | 30 min |
| 6 | Le Désert rocheux | Rock_desert | Existant | Solidify, mode combat, vaisseaux (renvoi des bombes) | 1 h |
| 7 | Le Désert de lave | Desert_lava | Existant, à terminer | Lave, Solidify poussé à fond | 1 h |
| 8 | La Forêt des échos | *nouveau* | Nouveau | Niveau entièrement bâti sur les dimensions | 1 h |
| 9 | Retour à la Prison | Prison of the Terioriams (2e moitié) | Existant, à scinder | Libérer les derniers Arialiens, voler l'accès à la citadelle | 50 min |
| 10 | Arial en ruines | *nouveau* (île, maisons, pont du prototype) | Nouveau, base existante | Le monde d'origine de Lumka, dernières énigmes | 1 h |
| 11 | La Citadelle impériale | *nouveau* | Nouveau | Ascension et combat contre l'Empereur | 1 h |
| — | Épilogue | Final_game, « And Then and Then » | Existant | Arial rallumée, crédits | 10 min |

**Total** : environ 12 h en ligne droite, 15 h en cherchant tous les Clims. 10 niveaux sur 13 existent déjà
dans le `.blend`, ce qui confirme qu'on peut réutiliser les modélisations de Naball.

**Répartition des 200 Clims** (proposition) : Ger 20, Prairie 20, Marais 20, Monts 12, Cité 12, Mines 6,
Désert rocheux 12, Désert de lave 10, Forêt des échos 10, Prison 5, Arial 3, soit 130 posés, plus 70
donnés par les 7 Arialiens.

**Portails du hub** : un portail par chapitre (1 à 8), ouverts dans l'ordre. Les chapitres 9 à 11 passent
par un portail de la Prison qui ne s'ouvre qu'après la Forêt des échos : c'est ce qui explique le retour en
Prison (incohérence 6) et donne un rôle au portail scellé (incohérence 3). Certains portails demandent un
nombre minimal de Clims pour s'ouvrir, comme l'annonçait Helpi (« ils te permettront d'accéder à des lieux cachés »).

## 5. Ce que ça change pour le remake

- Réécrire les dialogues de `Assets/lang` pour Lumka et les nouvelles commandes (ordre des pouvoirs, Clims violets).
- Rendre le vol au Clim violet et faire du double saut un pouvoir gagné (Monts Célestes).
- Scinder la Prison en prologue et chapitre 9 ; relier Swamp_intro et Swamp_bug ; terminer Mountain_high et Desert_lava.
- Refaire Underground à la main en cité (son `WorldGenerator` n'a pas d'équivalent Unity).
- Nouveaux besoins de modélisation : la Forêt des échos, Arial en ruines (partir de la scène `Test_arial` du prototype), la Citadelle, l'Empereur.

## Décisions attendues d'Alan

1. Naball dans l'histoire : option A (ancien gardien, recommandée) ou B (disparaît) ?
2. Le vol : Clim violet comme dans l'original, et double saut en pouvoir gagné plus tard (recommandé) ?
3. L'ordre des chapitres du tableau 4, en particulier les Monts Célestes avant la Cité ?
