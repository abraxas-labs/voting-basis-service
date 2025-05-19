# ✨ Changelog (`v2.54.6`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.54.6
Previous version ---- v2.43.0
Initial version ----- v1.27.15
Total commits ------- 28
```

## [v2.54.6] - 2025-04-15

### ❌ Removed

- Injections for PoliticalAssemblyStateSetPastJob and PoliticalAssemblyStateArchiveJob. Will be reapplied after rollout of eventprocessing for past lock and archive of political assemblies in Stimmunterlagen.

## [v2.54.5] - 2025-04-15

### 🔄 Changed

- fix missing domain of influence post data

## [v2.54.4] - 2025-04-14

### 🔄 Changed

- delete domain of influence related data for backward compatibility

## [v2.54.3] - 2025-04-14

### 🔄 Changed

- fix existing political assembly states

## [v2.54.2] - 2025-04-11

### 🆕 Added

- PastLockedPoliticalAssemblyJob, job to change state of political assemblies to passed
- ArchivePoliticalAssemblyJob, job to change state of political assemblies to archived
- properties State, ArchivePer, PastLockPer and fuctions TrySetPastLocked(), TryArchive(), Archive(DateTime? archivePer = null) to PoliticalAssemblyAgregate
- functions Archive(), TrySetPastLocked(), TryArchive() to PoliticalAssemblyWriter
- PoliticalAssemblyStateExtension
- Model PoliticalAssemblyState
- endpoint Archive() in PoliticalAssemblyService

### 🔄 Changed

- mapping in PoliticalAssemblyProfile
- function List() in PoliticalAssemblyReader - selection enhanced with state
- PoliticalAssemblyModelBuilder, added Property ArchivePer and PastLockPer. added Index for doi and state
- Model PoliticalAssembly, added State, ArchivePer and PastLockPer

## [v2.54.1] - 2025-03-31

### 🔄 Changed

- check for unqiue political business number also for secondary majority elections

## [v2.54.0] - 2025-03-27

### 🆕 Added

- add domain of influence franking licence away number

## [v2.53.0] - 2025-03-26

### 🆕 Added

- add e-collecting settings on dois

## [v2.52.0] - 2025-03-14

### 🆕 Added

- add country, street and house number to election candidate

## [v2.51.3] - 2025-03-13

### ❌ Removed

- Enum Values Chamois and Gold in VotingCardColor

## [v2.51.2] - 2025-03-11

### 🔄 Changed

- soft-delete domain of influences and cleanly delete related entities

## [v2.51.1] - 2025-03-06

### 🔄 Changed

- add tests for doi hierarchy and permissions changes
- improve domain of influence permission and hierarchy event processing speed

## [v2.51.0] - 2025-03-05

### 🔄 Changed

- do not publish event processed messages for catch-ups

## [v2.50.1] - 2025-03-04

### 🔄 Changed

- ensure valid majority election ballot groups

## [v2.50.0] - 2025-02-28

### 🔄 Changed

- switched to a more generic event watching mechanism

## [v2.49.1] - 2025-02-27

### 🔄 Changed

- Allow creation of political assemblies when DOI is not responsible for voting cards

## [v2.49.0] - 2025-02-25

### 🆕 Added

- add e-collecting flag

## [v2.48.1] - 2025-02-20

### 🔄 Changed

- improve ballot group ux

## [v2.48.0] - 2025-02-18

### 🆕 Added

- add domain of influence multiple electoral register flag

## [v2.47.1] - 2025-02-14

### :arrows_counterclockwise: Changed

- check political business number uniqueness per pb type

## [v2.47.0] - 2025-02-14

### :new: Added

- added hide occupation title canton setting

## [v2.46.0] - 2025-02-14

### 🆕 Added

- add ecounting flag to counting circles

## [v2.45.0] - 2025-02-14

### 🔄 Changed

- allow to export all political businesses one has read access to

## [v2.44.0] - 2025-02-13

### :new: Added

- validate eCH files on export

## [v2.43.1] - 2025-02-12

### 🔄 Changed

- validate max length for short and official descriptions in ballot according to eCH-0155 v4.0

### 🔄 Changed

- add candidates to secondary majority election bugfixes

## [v2.43.0] - 2025-02-06

### :new: Added

- added hide lower domain of influences in reports flag

## [v2.42.1] - 2025-01-10

### 🔄 Changed

- update voting library from 12.20.0 to 12.22.3

### 🔒 Security

- use updated Pkcs11Interop library version 5.2.0

## [v2.42.0] - 2025-01-10

### :arrows_counterclockwise: Changed

- restrict admin permissions

## [v2.41.1] - 2025-01-10

### 🔄 Changed

- improve counting circle and domain of influence event processing performance

## [v2.41.0] - 2024-12-18

### 🔄 Changed

- update minio lib and testcontainer according to latest operated version

## [v2.40.0] - 2024-12-16

### 🆕 Added

- include user id in log output

## [v2.39.0] - 2024-12-16

### 🆕 Added

- add secondary majority election protocols

## [v2.38.0] - 2024-12-11

### 🆕 Added

- domain of influence voting card flat rate owner

## [v2.37.4] - 2024-12-11

### 🔄 Changed

- majority election candidate optional values in active contest

## [v2.37.3] - 2024-12-09

### :arrows_counterclockwise: Changed

- correctly export occupation title in eCH

## [v2.37.2] - 2024-11-29

### 🔄 Changed

- export political lastname as family name

## [v2.37.1] - 2024-11-29

### 🔄 Changed

- move resolve contest import from grpc to rest

## [v2.37.0] - 2024-11-28

### :new: Added

- added read-only roles

## [v2.36.0] - 2024-11-27

### ❌ Removed

- remove allowed candidates from secondary majority elections

### 🔄 Changed

- optimize SourceLink integration and use new ci/cd versioning capabilities
- prevent duplicated commit ids in product version, only use SourceLink plugin.
- extend .dockerignore file with additional exclusions

### 🔄 Changed

- only enable virtual top level on root domain of influence and restrict superior authority types

### 🆕 Added

- feat(VOTING-4526): allow to overwrite majority reference candidate number

### 🔄 Changed

- feat(VOTING-4526): canton settings flag secondary majority election on separate ballot

### 🆕 Added

- publish results option on domain of influence

## [v2.35.2] - 2024-10-31

### ❌ Removed

- Injections for PoliticalAssemblyStateSetPastJob and PoliticalAssemblyStateArchiveJob. Will be reapplied after rollout of eventprocessing for past lock and archive of political assemblies in Stimmunterlagen.

### 🔄 Changed

- fix missing domain of influence post data

### 🔄 Changed

- delete domain of influence related data for backward compatibility

### 🔄 Changed

- fix existing political assembly states

### 🆕 Added

- PastLockedPoliticalAssemblyJob, job to change state of political assemblies to passed
- ArchivePoliticalAssemblyJob, job to change state of political assemblies to archived
- properties State, ArchivePer, PastLockPer and fuctions TrySetPastLocked(), TryArchive(), Archive(DateTime? archivePer = null) to PoliticalAssemblyAgregate
- functions Archive(), TrySetPastLocked(), TryArchive() to PoliticalAssemblyWriter
- PoliticalAssemblyStateExtension
- Model PoliticalAssemblyState
- endpoint Archive() in PoliticalAssemblyService

### 🔄 Changed

- mapping in PoliticalAssemblyProfile
- function List() in PoliticalAssemblyReader - selection enhanced with state
- PoliticalAssemblyModelBuilder, added Property ArchivePer and PastLockPer. added Index for doi and state
- Model PoliticalAssembly, added State, ArchivePer and PastLockPer

### 🔄 Changed

- check for unqiue political business number also for secondary majority elections

### 🆕 Added

- add domain of influence franking licence away number

### 🆕 Added

- add e-collecting settings on dois

### 🆕 Added

- add country, street and house number to election candidate

### ❌ Removed

- Enum Values Chamois and Gold in VotingCardColor

### 🔄 Changed

- soft-delete domain of influences and cleanly delete related entities

### 🔄 Changed

- add tests for doi hierarchy and permissions changes
- improve domain of influence permission and hierarchy event processing speed

### 🔄 Changed

- do not publish event processed messages for catch-ups

### 🔄 Changed

- ensure valid majority election ballot groups

### 🔄 Changed

- switched to a more generic event watching mechanism

### 🔄 Changed

- Allow creation of political assemblies when DOI is not responsible for voting cards

### 🆕 Added

- add e-collecting flag

### 🔄 Changed

- improve ballot group ux

### 🆕 Added

- add domain of influence multiple electoral register flag

### :arrows_counterclockwise: Changed

- check political business number uniqueness per pb type

### :new: Added

- added hide occupation title canton setting

### 🆕 Added

- add ecounting flag to counting circles

### 🔄 Changed

- allow to export all political businesses one has read access to

### :new: Added

- validate eCH files on export

### 🔄 Changed

- validate max length for short and official descriptions in ballot according to eCH-0155 v4.0

### 🔄 Changed

- add candidates to secondary majority election bugfixes

### :new: Added

- added hide lower domain of influences in reports flag

### 🔄 Changed

- update voting library from 12.20.0 to 12.22.3

### 🔒 Security

- use updated Pkcs11Interop library version 5.2.0

### :arrows_counterclockwise: Changed

- restrict admin permissions

### 🔄 Changed

- update minio lib and testcontainer according to latest operated version

### 🆕 Added

- include user id in log output

### 🆕 Added

- add secondary majority election protocols

### 🆕 Added

- domain of influence voting card flat rate owner

### 🔄 Changed

- majority election candidate optional values in active contest

### :arrows_counterclockwise: Changed

- correctly export occupation title in eCH

### 🔄 Changed

- export political lastname as family name

### 🔄 Changed

- move resolve contest import from grpc to rest

### :new: Added

- added read-only roles

### ❌ Removed

- remove allowed candidates from secondary majority elections

### 🔄 Changed

- optimize SourceLink integration and use new ci/cd versioning capabilities
- prevent duplicated commit ids in product version, only use SourceLink plugin.
- extend .dockerignore file with additional exclusions

### 🔄 Changed

- only enable virtual top level on root domain of influence and restrict superior authority types

### 🆕 Added

- feat(VOTING-4526): allow to overwrite majority reference candidate number

### 🔄 Changed

- feat(VOTING-4526): canton settings flag secondary majority election on separate ballot

### 🆕 Added

- publish results option on domain of influence
