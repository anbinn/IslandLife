# IslandLife Engine Decision

Version: T0.2
Owner: PM

## Purpose

Define the technology direction before implementation.

## Platform Direction

Primary target:
- Douyin mini game initial release.

Future consideration:
- Android
- iOS

## Engine Decision

Selected Engine:
- Unity

License:
- Unity Personal for current development stage.

## Engine Version

Target:
- Unity 6.3 LTS

Locked Version:
- 6000.3.25f1

The editor version is verified and locked. All development environments must use 6000.3.25f1.

## Development Stack

Language:
- C#

Required Environment:
- Unity Hub
- Unity Editor
- Visual Studio / compatible C# IDE
- Git + GitHub

## Decision Reasoning

Unity is selected because:

- Strong mobile game development ecosystem.
- Large amount of reusable assets and tooling.
- Suitable for 2D/isometric presentation.
- Supports future Android and iOS expansion.
- Good compatibility with AI assisted development workflows.

## Platform And Expansion Rules

The initial version focuses on Douyin mini game delivery.
The project architecture must keep future Android/iOS expansion possible.

## Decision Status

Engine selection is frozen.
Production development can begin after local environment verification.
