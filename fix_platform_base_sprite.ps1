$ErrorActionPreference = "Stop"

function Clear-GroundPlatformRootSprites {
    param(
        [string]$Path
    )

    $fullPath = (Resolve-Path $Path).Path
    $lines = [System.Collections.Generic.List[string]](Get-Content $fullPath)

    $gameObjectNames = @{}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!1 &(\d+)$') {
            $gameObjectId = $matches[1]
            for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_Name: (.+)$') {
                    $gameObjectNames[$gameObjectId] = $matches[1]
                    break
                }
            }
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!212 &') {
            $gameObjectId = $null
            $spriteLineIndex = $null

            for ($j = $i + 1; $j -lt [Math]::Min($i + 80, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_GameObject: \{fileID: (\d+)\}$') {
                    $gameObjectId = $matches[1]
                }

                if ($lines[$j] -match '^  m_Sprite: ') {
                    $spriteLineIndex = $j
                    break
                }
            }

            if (-not $gameObjectId -or $null -eq $spriteLineIndex) {
                continue
            }

            $gameObjectName = $gameObjectNames[$gameObjectId]
            if ($gameObjectName -like 'GroundPlatform*') {
                $lines[$spriteLineIndex] = '  m_Sprite: {fileID: 0}'
            }
        }
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $content = [string]::Join("`n", $lines)
    [System.IO.File]::WriteAllText($fullPath, $content + "`n", $utf8NoBom)
}

function Clear-NamedObjectSprites {
    param(
        [string]$Path,
        [string[]]$NamePatterns
    )

    $fullPath = (Resolve-Path $Path).Path
    $lines = [System.Collections.Generic.List[string]](Get-Content $fullPath)

    $gameObjectNames = @{}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!1 &(\d+)$') {
            $gameObjectId = $matches[1]
            for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_Name: (.+)$') {
                    $gameObjectNames[$gameObjectId] = $matches[1]
                    break
                }
            }
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!212 &') {
            $gameObjectId = $null
            $spriteLineIndex = $null

            for ($j = $i + 1; $j -lt [Math]::Min($i + 80, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_GameObject: \{fileID: (\d+)\}$') {
                    $gameObjectId = $matches[1]
                }

                if ($lines[$j] -match '^  m_Sprite: ') {
                    $spriteLineIndex = $j
                    break
                }
            }

            if (-not $gameObjectId -or $null -eq $spriteLineIndex) {
                continue
            }

            $gameObjectName = $gameObjectNames[$gameObjectId]
            foreach ($pattern in $NamePatterns) {
                if ($gameObjectName -like $pattern) {
                    $lines[$spriteLineIndex] = '  m_Sprite: {fileID: 0}'
                    break
                }
            }
        }
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $content = [string]::Join("`n", $lines)
    [System.IO.File]::WriteAllText($fullPath, $content + "`n", $utf8NoBom)
}

function Set-NamedObjectSprites {
    param(
        [string]$Path,
        [string[]]$NamePatterns,
        [string]$SpriteValue
    )

    $fullPath = (Resolve-Path $Path).Path
    $lines = [System.Collections.Generic.List[string]](Get-Content $fullPath)

    $gameObjectNames = @{}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!1 &(\d+)$') {
            $gameObjectId = $matches[1]
            for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_Name: (.+)$') {
                    $gameObjectNames[$gameObjectId] = $matches[1]
                    break
                }
            }
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!212 &') {
            $gameObjectId = $null
            $spriteLineIndex = $null

            for ($j = $i + 1; $j -lt [Math]::Min($i + 80, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_GameObject: \{fileID: (\d+)\}$') {
                    $gameObjectId = $matches[1]
                }

                if ($lines[$j] -match '^  m_Sprite: ') {
                    $spriteLineIndex = $j
                    break
                }
            }

            if (-not $gameObjectId -or $null -eq $spriteLineIndex) {
                continue
            }

            $gameObjectName = $gameObjectNames[$gameObjectId]
            foreach ($pattern in $NamePatterns) {
                if ($gameObjectName -like $pattern) {
                    $lines[$spriteLineIndex] = "  m_Sprite: $SpriteValue"
                    break
                }
            }
        }
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $content = [string]::Join("`n", $lines)
    [System.IO.File]::WriteAllText($fullPath, $content + "`n", $utf8NoBom)
}

function Set-NamedObjectColors {
    param(
        [string]$Path,
        [string[]]$NamePatterns,
        [string]$ColorValue
    )

    $fullPath = (Resolve-Path $Path).Path
    $lines = [System.Collections.Generic.List[string]](Get-Content $fullPath)

    $gameObjectNames = @{}
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!1 &(\d+)$') {
            $gameObjectId = $matches[1]
            for ($j = $i + 1; $j -lt [Math]::Min($i + 20, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_Name: (.+)$') {
                    $gameObjectNames[$gameObjectId] = $matches[1]
                    break
                }
            }
        }
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^--- !u!212 &') {
            $gameObjectId = $null
            $colorLineIndex = $null

            for ($j = $i + 1; $j -lt [Math]::Min($i + 90, $lines.Count); $j++) {
                if ($lines[$j] -match '^  m_GameObject: \{fileID: (\d+)\}$') {
                    $gameObjectId = $matches[1]
                }

                if ($lines[$j] -match '^  m_Color: ') {
                    $colorLineIndex = $j
                    break
                }
            }

            if (-not $gameObjectId -or $null -eq $colorLineIndex) {
                continue
            }

            $gameObjectName = $gameObjectNames[$gameObjectId]
            foreach ($pattern in $NamePatterns) {
                if ($gameObjectName -like $pattern) {
                    $lines[$colorLineIndex] = "  m_Color: $ColorValue"
                    break
                }
            }
        }
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $content = [string]::Join("`n", $lines)
    [System.IO.File]::WriteAllText($fullPath, $content + "`n", $utf8NoBom)
}

Clear-GroundPlatformRootSprites -Path 'Assets\Scenes\Level_01.unity'
Clear-GroundPlatformRootSprites -Path 'Assets\Prefabs\GroundPlatform (1).prefab'
Set-NamedObjectSprites `
    -Path 'Assets\Scenes\Level_01.unity' `
    -NamePatterns @('Checkpoint*') `
    -SpriteValue '{fileID: 7482667652216324306, guid: 311925a002f4447b3a28927169b83ea6, type: 3}'
Set-NamedObjectColors `
    -Path 'Assets\Scenes\Level_01.unity' `
    -NamePatterns @('background_0') `
    -ColorValue '{r: 0.52, g: 0.36, b: 0.36, a: 0.88}'
