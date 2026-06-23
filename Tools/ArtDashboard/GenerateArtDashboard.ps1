param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$SkipSamples
)

Add-Type -AssemblyName System.Drawing

$dashboardRoot = Join-Path $ProjectRoot 'Tools\ArtDashboard'
$sampleRoot = Join-Path $dashboardRoot 'test-png'
$manifestPath = Join-Path $dashboardRoot 'art_manifest.json'
$manifestJsPath = Join-Path $dashboardRoot 'art_manifest.js'

New-Item -ItemType Directory -Force -Path $dashboardRoot | Out-Null
New-Item -ItemType Directory -Force -Path $sampleRoot | Out-Null

$assets = [ordered]@{}

function Convert-GodotPathToRelativePath {
    param([string]$GodotPath)
    return $GodotPath.Replace('res://', '').Replace('/', '\')
}

function Get-DefaultsForCategory {
    param([string]$Category)
    switch ($Category) {
        'UI Background' { return @{ Width = 1080; Height = 1920; Transparent = $false; NineSlice = $false } }
        'UI Panel' { return @{ Width = 96; Height = 96; Transparent = $true; NineSlice = $true } }
        'UI Button' { return @{ Width = 96; Height = 48; Transparent = $true; NineSlice = $true } }
        'Town Facility' { return @{ Width = 96; Height = 96; Transparent = $true; NineSlice = $false } }
        'Dungeon Icon' { return @{ Width = 96; Height = 96; Transparent = $true; NineSlice = $false } }
        'HUD Icon' { return @{ Width = 64; Height = 64; Transparent = $true; NineSlice = $false } }
        'Equipment Icon' { return @{ Width = 64; Height = 64; Transparent = $true; NineSlice = $false } }
        'Actor Animation' { return @{ Width = 96; Height = 96; Transparent = $true; NineSlice = $false } }
        default { return @{ Width = 96; Height = 96; Transparent = $true; NineSlice = $false } }
    }
}

function Get-AssetSize {
    param(
        [string]$RelativePath,
        [int]$FallbackWidth,
        [int]$FallbackHeight
    )
    $path = Join-Path $ProjectRoot $RelativePath
    if (Test-Path -LiteralPath $path) {
        $image = [System.Drawing.Image]::FromFile($path)
        try {
            return @{ Width = $image.Width; Height = $image.Height; Exists = $true; Bytes = (Get-Item -LiteralPath $path).Length }
        }
        finally {
            $image.Dispose()
        }
    }
    return @{ Width = $FallbackWidth; Height = $FallbackHeight; Exists = $false; Bytes = 0 }
}

function Normalize-PromptText {
    param([string]$Text)
    return ($Text -replace '\s+', ' ').Trim().TrimEnd('.', ' ', "`t")
}

function Get-DungeonIconSubjectGuidance {
    param(
        [string]$Title,
        [string]$RelativePath
    )

    $key = [System.IO.Path]::GetFileNameWithoutExtension($RelativePath).Replace('dungeon_', '').ToLowerInvariant()
    switch ($key) {
        'chest' {
            return 'Subject: chest-training route symbol, a simplified pectoral shield or sternum-and-rib silhouette. Use two or three large shapes only. Forbidden: full torso, armor breastplate, character body, monster chest, dungeon gate, decorative badge frame.'
        }
        'shoulders' {
            return 'Subject: shoulder-training route symbol, two simplified deltoid caps or shoulder-plate arcs around a small center mark. Forbidden: full torso, head, arms, armor suit, spiked crest, dungeon gate, decorative badge frame.'
        }
        'back' {
            return 'Subject: back-training route symbol, a simplified spine line with two shoulder-blade shapes, or a compact upper-back emblem made from broad clean strokes. Forbidden: full muscular back illustration, torso, armor backplate, monster body, dungeon doorway, large heraldic crest, decorative badge frame.'
        }
        'legs' {
            return 'Subject: leg-training route symbol, simplified bent leg, boot-step, or paired thigh/calf shapes. Forbidden: full body, running character, pants detail, armor suit, dungeon gate, decorative badge frame.'
        }
        'core' {
            return 'Subject: core-training route symbol, simplified abdominal/core ring, waist brace, or central power knot. Forbidden: full torso, six-pack anatomy illustration, character body, armor chest, dungeon gate, decorative badge frame.'
        }
        'arms' {
            return 'Subject: arm-training route symbol, simplified forearm, flexed arm silhouette, or dumbbell-like training mark. Forbidden: full body, portrait, complex weapon, armor suit, dungeon gate, decorative badge frame.'
        }
        default {
            return "Subject: compact body-part training route symbol for $Title. Forbidden: full body, character illustration, dungeon scene, large heraldic crest, decorative badge frame."
        }
    }
}

function Get-CategoryPromptGuidance {
    param(
        [string]$Category,
        [string]$Title,
        [string]$RelativePath,
        [int]$Width,
        [int]$Height
    )
    switch ($Category) {
        'UI Background' {
            return 'Full-bleed vertical mobile background, not an illustration card. Leave broad calm readable zones for live UI overlays near the center, top, and bottom. Use depth, atmosphere, and readable silhouettes. Do not add labels, readable signs, character portraits, inventory items, floating buttons, frames, or cropped stock-photo composition.'
        }
        'UI Panel' {
            return 'Transparent UI frame or panel surface only. Preserve a clean empty center for live Godot text and controls. Make corners distinct, edges straight/repeatable, and ornament low-density. Do not create a full screen, emblem crest, card illustration, background scene, or text-bearing plaque.'
        }
        'UI Button' {
            return 'Transparent button body only, no label. Keep the center flat/clean for live Godot text. Use simple bevels, highlights, and edge treatment that remain stable when stretched horizontally. Do not add icons, text, symbols, full UI panels, or decorative frame extensions.'
        }
        'Town Facility' {
            return 'Centered clickable town facility icon on transparent background. Show one building or object silhouette clearly at small mobile size, occupying about 70 percent of the canvas. No surrounding square tile, no full town scene, no readable sign text, no character, no scenic background.'
        }
        'Dungeon Icon' {
            $subject = Get-DungeonIconSubjectGuidance $Title $RelativePath
            return "Compact UI route symbol, not a fantasy illustration. $subject Use 2-4 broad shapes, 8px transparent safe padding, and a silhouette readable at 48px. Subject should occupy about 68-78 percent of the 128px canvas. No letters, labels, scenery, frame, portal, doorway, or detailed anatomy."
        }
        'HUD Icon' {
            return 'Small HUD icon on transparent background. Use one bold simple silhouette, high contrast, clean alpha edges, and no more than a few internal highlights. Avoid tiny details, background tiles, labels, decorative frames, or full item scenes.'
        }
        'Equipment Icon' {
            return 'Single equipment item only on transparent background. Center it with a clean three-quarter game-icon angle, one readable silhouette, and restrained material accents. No inventory slot frame, no background tile, no character holding it, no scene, no text.'
        }
        'Actor Animation' {
            if ($Width -gt ($Height * 2)) {
                $frameCount = [Math]::Max(2, [Math]::Round($Width / [Math]::Max(1, $Height)))
                $cellWidth = [Math]::Round($Width / $frameCount)
                return "Horizontal sprite strip with exactly $frameCount evenly spaced animation frames, each about ${cellWidth}x${Height}px. Keep the same character scale, anchor point, ground line, lighting, and facing direction across every frame. No frame dividers, labels, background scenery, giant portrait, cropped limbs, or shadows touching the canvas edge."
            }
            return 'Single centered combat sprite frame on transparent background. Keep a consistent anchor point, clean silhouette, readable pose, and enough padding for animation without cropping. No portrait crop, no background, no frame, no UI badge.'
        }
        default {
            return 'Centered game asset with a clear silhouette, clean alpha, and mobile-readable detail.'
        }
    }
}

function Get-CategoryNegativePrompt {
    param([string]$Category)
    switch ($Category) {
        'Dungeon Icon' {
            return 'no full body, no muscular torso illustration, no armor plate, no monster anatomy, no dungeon doorway, no portal, no heraldic crest, no badge frame, no scene background, no tiny noisy detail'
        }
        'UI Background' {
            return 'no UI buttons, no panels, no readable signs, no labels, no character portrait, no object collage, no border frame'
        }
        'UI Panel' {
            return 'no full scene, no filled center illustration, no readable text, no icons in the center, no asymmetrical broken edges'
        }
        'UI Button' {
            return 'no label text, no icon, no complex border, no complete menu, no panel background'
        }
        'Actor Animation' {
            return 'no frame dividers, no background, no portrait crop, no inconsistent scale, no inconsistent pose anchor, no cropped weapon or limb'
        }
        default {
            return 'no full scene, no text, no letters, no numbers, no watermark, no logo, no random symbols, no background tile'
        }
    }
}

function New-ArtPrompt {
    param(
        [string]$Category,
        [string]$Title,
        [string]$RelativePath,
        [string]$Role,
        [string]$Look,
        [int]$Width,
        [int]$Height,
        [bool]$Transparent,
        [bool]$NineSlice
    )

    $backgroundRule = if ($Transparent) {
        'Transparent PNG with alpha channel. Subject only; no colored backdrop, no square tile, no halo clipped by the canvas.'
    }
    else {
        'Opaque full-bleed PNG. Fill the entire canvas; no transparent holes, no UI text, no baked interface widgets.'
    }
    $sliceRule = if ($NineSlice) {
        'Nine-slice friendly. Put most ornament in the corners, keep edges straight/repeatable, and keep the center quiet enough for Godot text and controls.'
    }
    else {
        'Not a nine-slice frame. Do not add decorative borders unless the role explicitly asks for a frame.'
    }
    $roleText = Normalize-PromptText $Role
    $lookText = Normalize-PromptText $Look
    $categoryGuidance = Get-CategoryPromptGuidance $Category $Title $RelativePath $Width $Height
    $categoryNegative = Get-CategoryNegativePrompt $Category

    return @(
        "Create exactly one production-ready DungeonFit 2D game asset PNG.",
        "Asset: $Title.",
        "Category: $Category.",
        "In-game role: $roleText.",
        "Visual target: $lookText.",
        "Canvas: exactly ${Width}x${Height}px. Do not resize, crop, pad, upscale, or add extra margins.",
        "Background rule: $backgroundRule",
        "Layout rule: $categoryGuidance",
        "Stretch rule: $sliceRule",
        "Style target: dark moonlit fantasy RPG UI asset, simplified painterly rendering, crisp mobile silhouette, deep indigo/purple shadows, restrained warm gold rim light, clean alpha edge, polished game-asset finish. Prefer clarity over detail.",
        "Negative prompt: $categoryNegative, no readable text, no letters, no numbers, no watermarks, no signatures, no logos, no UI labels, no extra objects outside the role, no muddy blur, no low-resolution artifacts, no cropped edges."
    ) -join "`n"
}

function Add-ArtAsset {
    param(
        [string]$GodotPath,
        [string]$Category,
        [string]$Title,
        [string]$Role,
        [string]$Look,
        [string]$Notes = '',
        [string]$Priority = 'P1',
        [Nullable[bool]]$Transparent = $null,
        [Nullable[bool]]$NineSlice = $null
    )

    if ([string]::IsNullOrWhiteSpace($GodotPath) -or !$GodotPath.EndsWith('.png')) {
        return
    }

    $relativePath = Convert-GodotPathToRelativePath $GodotPath
    $key = $relativePath.Replace('\', '/')
    if ($assets.Contains($key)) {
        return
    }

    $defaults = Get-DefaultsForCategory $Category
    if ($Priority -eq 'P1') {
        if ($Category -in @('UI Background', 'UI Panel', 'UI Button', 'Town Facility', 'Dungeon Icon')) {
            $Priority = 'P0'
        }
        elseif ($Category -in @('Equipment Icon', 'HUD Icon')) {
            $Priority = 'P1'
        }
        else {
            $Priority = 'P2'
        }
    }
    $size = Get-AssetSize $relativePath $defaults.Width $defaults.Height
    $sampleRelativePath = Join-Path 'test-png' $relativePath
    $transparentValue = if ($Transparent.HasValue) { $Transparent.Value } else { $defaults.Transparent }
    $nineSliceValue = if ($NineSlice.HasValue) { $NineSlice.Value } else { $defaults.NineSlice }
    $prompt = New-ArtPrompt $Category $Title $key $Role $Look $size.Width $size.Height $transparentValue $nineSliceValue
    $assets[$key] = [ordered]@{
        id = ($key -replace '[^A-Za-z0-9]+', '_').Trim('_').ToLowerInvariant()
        title = $Title
        category = $Category
        priority = $Priority
        godotPath = $GodotPath
        relativePath = $key
        expectedWidth = $size.Width
        expectedHeight = $size.Height
        transparent = $transparentValue
        nineSlice = $nineSliceValue
        exists = $size.Exists
        currentBytes = $size.Bytes
        productionStatus = 'Needs final art'
        generatedPrompt = $prompt
        finalChecks = @(
            [ordered]@{ id = 'size_correct'; label = 'Size correct'; required = "$($size.Width)x$($size.Height) px" },
            [ordered]@{ id = 'transparent_background'; label = 'Transparent background'; required = if ($transparentValue) { 'Required' } else { 'Not required' } },
            [ordered]@{ id = 'no_baked_text'; label = 'No baked text'; required = 'Required' },
            [ordered]@{ id = 'clean_edges'; label = 'Clean edges'; required = 'Required' },
            [ordered]@{ id = 'godot_import_tested'; label = 'Godot import tested'; required = 'Required' }
        )
        samplePath = $sampleRelativePath.Replace('\', '/')
        role = $Role
        look = $Look
        notes = $Notes
    }
}

Add-ArtAsset 'res://Assets/Art/UI/Common/bg_common.png' 'UI Background' 'Common background' 'Shared background for Tavern, Church, shops, and utility pages.' 'Dark moonlit town or dungeon ambience, no baked UI text.'
Add-ArtAsset 'res://Assets/Art/UI/Common/panel_main.png' 'UI Panel' 'Main panel frame' 'Primary large frame used for headers and major page blocks.' 'Ornate purple and gold frame, stretch-safe corners.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/Common/panel_card.png' 'UI Panel' 'Card panel frame' 'Repeated cards, small detail panes, tokens, inventory cells.' 'Compact ornate frame, less visual weight than main panel.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/Common/button_primary.png' 'UI Button' 'Primary button' 'Main action button texture.' 'Bright purple and gold action treatment, no baked label text.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/Common/button_secondary.png' 'UI Button' 'Secondary button' 'Navigation and lower-priority action button texture.' 'Calmer blue or purple treatment, no baked label text.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/Common/button_danger.png' 'UI Button' 'Danger button' 'Destructive or risky action button texture.' 'Dark crimson or purple warning treatment, no baked label text.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/Common/bar_fill.png' 'HUD Icon' 'Progress bar fill' 'Reusable bar fill texture for EXP and HP-like meters.' 'Clean glowing fill strip, tileable or stretch-safe.'

Add-ArtAsset 'res://Assets/Art/UI/Town/bg_town.png' 'UI Background' 'Town background' 'Town hub page background.' 'Moonlit fantasy town, readable dark foreground, no baked UI text.'
Add-ArtAsset 'res://Assets/Art/UI/Town/idle_token.png' 'Town Facility' 'Idle exploration token' 'Small icon for idle exploration panel.' 'Outdoor exploration symbol with lantern, forest, or moon cue.'

foreach ($entry in @(
    @('herb_shop', 'Herb shop', 'Clickable town facility for healing and potions.', 'Small apothecary or herb shop icon with green accent.'),
    @('tavern', 'Tavern', 'Clickable town facility for equipment management.', 'Warm tavern sign or building icon.'),
    @('blacksmith', 'Blacksmith', 'Clickable town facility for enhancement.', 'Forge, anvil, or weapon shop icon with metal accent.'),
    @('notice_board', 'Notice board', 'Clickable town facility for short quests.', 'Quest board with parchment, no readable baked text.'),
    @('fountain', 'Moonlight fountain', 'Clickable town facility for recovery and blessings.', 'Glowing moon fountain with blue or purple crystal light.'),
    @('church', 'Church', 'Clickable town facility for oath quests.', 'Moonlit chapel icon with purple stained-glass cue.')
)) {
    Add-ArtAsset "res://Assets/Art/UI/Town/$($entry[0]).png" 'Town Facility' $entry[1] $entry[2] $entry[3]
}

Add-ArtAsset 'res://Assets/Art/UI/DungeonPlan/bg_dungeon_plan.png' 'UI Background' 'Dungeon plan background' 'Dungeon route planning page background.' 'Portal or route planning scene, dark center, no baked route labels.'
Add-ArtAsset 'res://Assets/Art/UI/DungeonPlan/route_slot.png' 'UI Panel' 'Route slot frame' 'Repeated selected-route slot panel.' 'Small stretch-safe route card frame.' -NineSlice $true
foreach ($id in @('chest', 'shoulders', 'back', 'legs', 'core', 'arms')) {
    Add-ArtAsset "res://Assets/Art/UI/DungeonPlan/dungeon_$id.png" 'Dungeon Icon' "Dungeon icon: $id" 'Dungeon category icon for route selection.' 'Distinct training-bodypart dungeon symbol, readable at small card size.'
}

Add-ArtAsset 'res://Assets/Art/UI/RoomChallenge/bg_room.png' 'UI Background' 'Room challenge background' 'Workout combat room background.' 'Dungeon battle space, dark but lively, no baked UI text.'
Add-ArtAsset 'res://Assets/Art/UI/RoomChallenge/battle_stage.png' 'UI Panel' 'Battle stage panel' 'Frame or backdrop behind player and enemy tokens.' 'Stage-like combat arena frame, stretch-safe if possible.'
Add-ArtAsset 'res://Assets/Art/UI/RoomChallenge/actor_token.png' 'UI Panel' 'Actor token frame' 'Frame behind player and enemy sprites.' 'Circular or framed token slot, transparent center preferred.' -NineSlice $true
Add-ArtAsset 'res://Assets/Art/UI/RoomChallenge/potion.png' 'HUD Icon' 'Room potion icon' 'Small potion action icon in room UI.' 'Readable small potion bottle with healing cue.'

Add-ArtAsset 'res://Assets/Art/UI/Summary/bg_summary.png' 'UI Background' 'Summary background' 'Set and daily summary pages.' 'Reward or treasure room atmosphere, no baked UI text.'
Add-ArtAsset 'res://Assets/Art/UI/Summary/reward_chest.png' 'HUD Icon' 'Reward chest icon' 'Daily reward or chest summary icon.' 'Fantasy treasure chest, readable at small size.'
Add-ArtAsset 'res://Assets/Art/UI/Icons/gold.png' 'HUD Icon' 'Gold icon' 'Gold currency indicator.' 'Coin or gold symbol, high contrast.'
Add-ArtAsset 'res://Assets/Art/UI/Icons/exp.png' 'HUD Icon' 'EXP icon' 'Experience indicator.' 'Star, spark, scroll, or EXP symbol.'
Add-ArtAsset 'res://Assets/Art/UI/Icons/potion.png' 'HUD Icon' 'Potion icon' 'General potion indicator.' 'Small readable potion bottle.'

foreach ($entry in @(
    @('Weapons/moon_blade.png', 'Moon blade', 'Weapon icon template.'),
    @('Weapons/war_hammer.png', 'War hammer', 'Weapon icon template.'),
    @('Weapons/training_bow.png', 'Training bow', 'Weapon icon template.'),
    @('Weapons/silver_dagger.png', 'Silver dagger', 'Weapon icon template.'),
    @('Armor/guard_plate.png', 'Guard plate', 'Armor icon template.'),
    @('Armor/iron_helm.png', 'Iron helm', 'Armor icon template.'),
    @('Armor/round_shield.png', 'Round shield', 'Armor icon template.'),
    @('Armor/stone_plate.png', 'Stone plate', 'Armor icon template.'),
    @('Accessories/oath_charm.png', 'Oath charm', 'Accessory icon template.'),
    @('Accessories/focus_crystal.png', 'Focus crystal', 'Accessory icon template.'),
    @('Accessories/golden_ring.png', 'Golden ring', 'Accessory icon template.'),
    @('Accessories/guard_medal.png', 'Guard medal', 'Accessory icon template.')
)) {
    Add-ArtAsset "res://Assets/Art/Items/$($entry[0])" 'Equipment Icon' $entry[1] $entry[2] 'Single item on transparent background, readable at 64 to 128 px.'
}

foreach ($entry in @(
    @('Player/Knight/Knight-Idle.png', 'Player knight idle'),
    @('Player/Knight/Knight-Attack01.png', 'Player knight attack'),
    @('Player/Knight/Knight-Hurt.png', 'Player knight hurt'),
    @('Player/Knight/Knight-Death.png', 'Player knight death')
)) {
    Add-ArtAsset "res://Assets/Art/Actors/$($entry[0])" 'Actor Animation' $entry[1] 'Player combat sprite animation frame or sheet.' 'Transparent fantasy hero sprite, consistent anchor and scale.'
}

$enemyIds = @(
    @{ Id = 'Skeleton'; Prefix = 'Skeleton-'; Files = @('Idle', 'Attack01', 'Hurt', 'Death', 'Block') },
    @{ Id = 'slime_basic'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'skeleton_basic'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death', 'block') },
    @{ Id = 'skeleton_archer'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'skeleton_armored'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'skeleton_greatsword'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'orc_basic'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'orc_armored'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death', 'block') },
    @{ Id = 'orc_elite'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'orc_rider_boss'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death', 'block') },
    @{ Id = 'axeman_armored'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'werewolf_boss'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') },
    @{ Id = 'werebear_boss'; Prefix = ''; Files = @('idle', 'attack_01', 'hurt', 'death') }
)

foreach ($enemy in $enemyIds) {
    foreach ($file in $enemy.Files) {
        $fileName = if ($enemy.Prefix) { "$($enemy.Prefix)$file.png" } else { "$file.png" }
        Add-ArtAsset "res://Assets/Art/Actors/Enemies/$($enemy.Id)/$fileName" 'Actor Animation' "$($enemy.Id) $file" 'Enemy combat sprite animation frame or sheet.' 'Transparent enemy sprite, consistent anchor and scale.'
    }
}

function New-SamplePng {
    param([hashtable]$Asset)
    $target = Join-Path $dashboardRoot $Asset.samplePath
    $targetDir = Split-Path -Path $target -Parent
    New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

    $width = [Math]::Max(16, [int]$Asset.expectedWidth)
    $height = [Math]::Max(16, [int]$Asset.expectedHeight)
    $bitmap = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        if (-not $Asset.transparent) {
            $graphics.Clear([System.Drawing.Color]::FromArgb(255, 22, 18, 42))
        }

        $categoryHash = [Math]::Abs($Asset.category.GetHashCode())
        $accent = [System.Drawing.Color]::FromArgb(255, 120 + ($categoryHash % 80), 80 + ($categoryHash % 110), 190 + ($categoryHash % 45))
        $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, 18, 14, 34))
        $linePen = New-Object System.Drawing.Pen($accent, [Math]::Max(2, [Math]::Min($width, $height) / 32))
        $thinPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 238, 210, 128), 1)
        $graphics.FillRectangle($bgBrush, 0, 0, $width, $height)
        $graphics.DrawRectangle($linePen, 1, 1, $width - 3, $height - 3)
        $graphics.DrawLine($thinPen, 0, $height / 2, $width, $height / 2)
        $graphics.DrawLine($thinPen, $width / 2, 0, $width / 2, $height)

        $fontSize = [Math]::Max(8, [Math]::Min(18, [Math]::Floor($width / 16)))
        $font = New-Object System.Drawing.Font('Consolas', $fontSize, [System.Drawing.FontStyle]::Bold)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 245, 230, 180))
        $smallBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 205, 195, 230))
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $label = ($Asset.category -replace ' ', "`n").ToUpperInvariant()
        $graphics.DrawString($label, $font, $textBrush, (New-Object System.Drawing.RectangleF(4, 4, ($width - 8), ($height - 8))), $format)
        $pathFont = New-Object System.Drawing.Font('Consolas', [Math]::Max(7, [Math]::Min(10, [Math]::Floor($width / 24))))
        $graphics.DrawString("$width x $height", $pathFont, $smallBrush, 4, [Math]::Max(4, $height - ($pathFont.Size * 2.2)))
    }
    finally {
        $graphics.Dispose()
        $bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
    }
}

$items = @($assets.Values)
if (-not $SkipSamples) {
    foreach ($item in $items) {
        New-SamplePng $item
    }
}

$summary = [ordered]@{
    generatedAt = (Get-Date).ToString('s')
    projectRoot = $ProjectRoot
    total = $items.Count
    categories = @($items | Group-Object { $_['category'] } | Sort-Object Name | ForEach-Object {
        [ordered]@{ category = $_.Name; count = $_.Count }
    })
}

$manifest = [ordered]@{
    summary = $summary
    assets = @($items | Sort-Object { $_['category'] }, { $_['relativePath'] })
}

$json = $manifest | ConvertTo-Json -Depth 10
Set-Content -LiteralPath $manifestPath -Value $json -Encoding UTF8
Set-Content -LiteralPath $manifestJsPath -Value "window.DUNGEONFIT_ART_MANIFEST = $json;" -Encoding UTF8

Write-Host "Generated $($items.Count) art specs"
Write-Host "Manifest: $manifestPath"
Write-Host "Dashboard: $(Join-Path $dashboardRoot 'index.html')"
