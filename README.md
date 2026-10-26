# Tail2Jump

Outil de suivi de logs en temps réel : surveillance de plusieurs fichiers, affichage
en continu, filtrage, recherche et ouverture du fichier dans l'éditeur à la ligne
indiquée.

## Deux implémentations

| Répertoire | Technologie | Plateformes |
|---|---|---|
| `Tail2Jump/` (historique) | WPF / .NET Framework 4.5.1 | Windows uniquement |
| `src/` (portage) | .NET 10 / Avalonia | Windows, Linux, macOS |

Le portage multiplateforme est **en cours**. La logique métier (tail de fichiers,
cache de lignes, moteur de surveillance, configuration) est déjà portée et testée ;
l'interface Avalonia arrive progressivement.

## Construire et tester le portage

Prérequis : SDK .NET 10.

```bash
dotnet build Tail2Jump.Avalonia.sln
dotnet test  Tail2Jump.Avalonia.sln
```

L'ancienne solution Windows (`Tail2Jump.sln`) reste construisible avec Visual Studio
sous Windows.

## Structure du portage

```
src/
  Tail2Jump.Core/        logique métier sans dépendance UI
  Tail2Jump.Core.Tests/  tests NUnit
```

La configuration est stockée en JSON dans `$XDG_CONFIG_HOME/Tail2Jump/SaveConfig.json`
(ou `~/.config/Tail2Jump/SaveConfig.json`). Le chemin peut être forcé avec la variable
d'environnement `TAIL2JUMP_CONFIG`.
