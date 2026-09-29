# Backlog du remake

Tenu par le skill `product-owner`. Priorités à valider par Alan.

### Tester le hub dans Unity 6
- **Pour qui / pourquoi** : En tant que joueur, je veux lancer le hub et le parcourir afin de valider la base du remake.
- **Critères d'acceptation** :
  - [ ] La scène se construit sans erreur à l'ouverture du projet
  - [ ] Lumka apparaît sur l'île, court, saute et ramasse un Clim
  - [ ] Le portail de Prairie_of_the waters affiche l'écran de chargement
- **Taille** : S · **Priorité** : Indispensable · **Statut** : À faire

### Régler les contrôles de Lumka en jeu
- **Pour qui / pourquoi** : En tant que joueur, je veux un personnage agréable à diriger afin de prendre plaisir à explorer.
- **Critères d'acceptation** :
  - [ ] Les plateformes du hub accessibles à la boule le sont à Lumka (saut, double saut)
  - [ ] La caméra ne passe pas à travers le décor du hub
- **Référence d'origine** : `LumkaController`, logic bricks de l'objet `Cube` de Ger_FieldSwamp
- **Taille** : M · **Priorité** : Indispensable · **Statut** : À faire

### Accroche aux rebords
- **Pour qui / pourquoi** : En tant que joueur, je veux que Lumka s'accroche aux rebords ratés de peu afin de moins rater mes sauts.
- **Critères d'acceptation** :
  - [ ] Un saut un peu court vers un rebord se termine accroché, puis Lumka se hisse
- **Référence d'origine** : animations `grimpe_rebord` et `supendu` de `lumka.fbx`
- **Taille** : M · **Priorité** : Important · **Statut** : À faire (à confirmer avec Alan)

### Porter Prairie_of_the waters
- **Pour qui / pourquoi** : En tant que joueur, je veux entrer dans le premier monde depuis le hub afin de continuer l'aventure.
- **Critères d'acceptation** :
  - [ ] Le portail du hub charge le niveau et le retour au hub fonctionne
  - [ ] Les Clims du niveau se ramassent et sont sauvegardés
- **Référence d'origine** : scène `Prairie_of_the waters` de `Assets/MAP_DATA.scenes`
- **Taille** : L (à découper après extraction) · **Priorité** : Important · **Statut** : À faire
