# Feuille de route du remake

Tenue par le skill `chef-de-projet`. État au 29/09/2026. Scénario et plan des niveaux proposés : `docs/scenario.md`.

| Jalon | État | Où |
|---|---|---|
| 1. Hub jouable avec Lumka | Livré, non testé en jeu | PR #2 |
| 2. Premier monde (Prairie_of_the waters, puis Swamp_intro / Swamp_bug) | À faire | |
| 3. Pouvoirs et ennemis (Eta, Solidify, Clims violets et rouges, Terioriams, Somtraj) | À faire | |
| 4. Mondes suivants (Underground, Rock_desert, Desert_lava, Mountain_high, Prison of the Terioriams, Cart_world, Chariot_mine, Final_game) | À faire | |
| 5. Enveloppe du jeu (menus, options, sauvegardes, cinématiques, HUD complet) | À faire | |

## Jalon 1 — Hub jouable avec Lumka

- [x] Chaîne d'extraction `.blend` → Unity (`Unity/Tools`)
- [x] Hub Ger_FieldSwamp : Clims, portails selon l'avancement, sons, dialogues
- [x] Lumka héros : modèle, animations, contrôleur retravaillé, caméra orbitale
- [ ] Ouvrir le projet dans Unity 6 et corriger ce qui ne marche pas
- [ ] Régler vitesses, saut et échelle de Lumka sur le parcours du hub

## Risques

- Échelle : les niveaux ont été conçus pour une boule de 2 m, Lumka mesure environ 2,2 m avec les oreilles.
- Rien n'a encore été vérifié dans l'éditeur Unity.
