# Outgoing Male – TODO

## 🔥 Tier 1 – Required for Public itch.io Release (v0.1)
- [x] Animate logo image
- [x] Animate logo text
- [x] Player sprite animation should be flapping even before game starts
- [x] Add Game Over text to game over screen

## ✨ Tier 2 – Strong Identity & UX Polish
- [ ] Create/implement sfx for hovering over/selecting buttons
- [x] Implement MainMenu music loop that will switch when the game is started
- [ ] Add options menu to game over menu
- [ ] Add character select option to game over menu
- [ ] Allow for player selection of palette swap from mainmenu and pause screen
- [x] Add panel/sprite behind audio mixer on options/pause menu for visibility
- [ ] Make clouds and street parallax start on game start
- [ ] Create death animation (rapid flap + fall offscreen)
- [ ] Add panel sprites behind menu items

## 🧩 Tier 3 – Light Progression (Optional for v0.1)
- [ ] Add temp menu/image to Character Select screen with single blacked-out unlockable palette swap
- [ ] Set point threshold for unlocking first cosmetic and test to see if unlock works properly
- [x] Create custom button sprite to use across UI screens

## 🚀 Tier 4 – Post‑Launch Systems
- [x] Create `StampDefinition` and `StampCatalog` data scripts
- [x] Create initial stamp data assets and stamp catalog entries
- [x] Implement collectible stamp mechanic using stamp sprites available, test
- [x] Create `StampPickup` prefab/component and hook into gameplay spawn/collection flow
- [ ] Add stamp collection sfx

## 🎨 Tier 5 – Visual Polish / Nice‑to‑Have
- [ ] Change ground/street colors to fit with game palette

## ✅ Recent Accomplishments (Unsorted)
- [x] Unified Game scene Options layout with MainMenu options layout
- [x] Replaced legacy text references in `GameManager` with TMP-compatible fields
- [x] Improved Ready-state UX: animated `Get Ready!` + blinking `StartPrompt`
- [x] Removed old StartButton/GameOverExtrude dependencies from gameplay flow
- [x] Fixed `DontDestroyOnLoad` warning in `SoundMixerManager`
- [x] Restored reliable start input from Ready screen (space/click/tap)
