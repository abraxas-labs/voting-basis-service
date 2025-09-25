# ✨ Changelog (`v2.66.3`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v2.66.3
Previous version ---- v2.54.6
Initial version ----- v1.27.15
Total commits ------- 32
```

## [v2.66.3] - 2025-09-16

### 🔄 Changed

- correctly build permissions when adding a new domain of influence

## [v2.66.2] - 2025-09-01

### 🔄 Changed

- correctly allow editing candidate references

## [v2.66.1] - 2025-08-26

### 🔄 Changed

- deactivate political business e-voting apporval temporarily

### 🔄 Changed

- deactivate political business e-voting apporval temporarily

## [v2.66.0] - 2025-08-25

### 🆕 Added

- add main voting cards domain of influence flag

## [v2.65.4] - 2025-08-14

### 🔄 Changed

- enable malware scanner

## [v2.65.3] - 2025-08-12

### 🔄 Changed

- fix update domain of influence voting card data as election admin

## [v2.65.2] - 2025-08-08

### 🔄 Changed

- correctly update candidate references on update to main candidate

## [v2.65.1] - 2025-08-08

### 🔄 Changed

- restrict locality to eCH length
- ensure correct swiss zip code

## [v2.65.0] - 2025-07-22

### 🆕 Added

- add sort number to admin management response

## [v2.64.1] - 2025-07-07

### 🔄 Changed

- prevent vote type change after ballot create

## [v2.64.0] - 2025-07-04

### 🔄 Changed

- set majority election candidate number on import

## [v2.63.0] - 2025-07-04

### 🔄 Added

- e-voting only contest export

## [v2.62.0] - 2025-07-03

### 🆕 Added

- add contest e-voting approval

## [v2.61.2] - 2025-07-02

### 🔄 Changed

- avoid throwing error when read all permission is available

### 🔒 Security

- reference third party images via harbor container registry proxy for better control over dependency management

## [v2.61.1] - 2025-07-02

### 🔄 Changed

- bump pkcs11 driver from 4.45 to 4.51.0.1

## [v2.61.0] - 2025-07-01

### 🔄 Changed

- prevent certain political business changes after create

## [v2.60.0] - 2025-06-25

### 🆕 Added

- add political business e-voting approve cron job

## [v2.59.0] - 2025-06-20

### 🆕 Added

- add e-voting approval on political businesses

## [v2.58.0] - 2025-06-18

### 🆕 Added

- add e-collecting email

## [v2.57.0] - 2025-06-17

### 🔄 Changed

- eCH export languages dependent of contest e-voting

## [v2.56.3] - 2025-06-11

### 🔄 Changed

- fix political business sub type include on political business summary detail

## [v2.56.2] - 2025-06-05

### 🔄 Changed

- fix political business sub type include on political business aummary list

## [v2.56.1] - 2025-05-27

### ❌ Removed

- remove electronic ballot question title from ech vote mapping

## [v2.56.0] - 2025-05-26

### 🔄 Changed

- refactor dockerfile and reduce cache layers

### 🔒 Security

- introduce user id and group id to avoid random assignment
- use exec form to avoid shell interpretation

## [v2.55.0] - 2025-05-23

### 🔄 Changed

- ensure candidate numbers have two digits in eCH exports

### 🆕 Added

- Injections for PoliticalAssemblyStateSetPastJob and PoliticalAssemblyStateArchiveJob. Activation of event generation for past lock and archive of political assemblies.

### 🔄 Changed

- initiative number of members committee is required for all dois

### 🆕 Added

- add Ech0157v5 and Ech0159v5

### 🆕 Added

- add e-collecting referendum and initiative properties

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

- correctly build permissions when adding a new domain of influence

### 🔄 Changed

- correctly allow editing candidate references

### 🔄 Changed

- deactivate political business e-voting apporval temporarily

### 🔄 Changed

- deactivate political business e-voting apporval temporarily

### 🆕 Added

- add main voting cards domain of influence flag

### 🔄 Changed

- enable malware scanner

### 🔄 Changed

- fix update domain of influence voting card data as election admin

### 🔄 Changed

- correctly update candidate references on update to main candidate

### 🔄 Changed

- restrict locality to eCH length
- ensure correct swiss zip code

### 🆕 Added

- add sort number to admin management response

### 🔄 Changed

- prevent vote type change after ballot create

### 🔄 Changed

- set majority election candidate number on import

### 🔄 Added

- e-voting only contest export

### 🆕 Added

- add contest e-voting approval

### 🔄 Changed

- avoid throwing error when read all permission is available

### 🔒 Security

- reference third party images via harbor container registry proxy for better control over dependency management

### 🔄 Changed

- bump pkcs11 driver from 4.45 to 4.51.0.1

### 🔄 Changed

- prevent certain political business changes after create

### 🆕 Added

- add political business e-voting approve cron job

### 🆕 Added

- add e-voting approval on political businesses

### 🆕 Added

- add e-collecting email

### 🔄 Changed

- eCH export languages dependent of contest e-voting

### 🔄 Changed

- fix political business sub type include on political business summary detail

### 🔄 Changed

- fix political business sub type include on political business aummary list

### ❌ Removed

- remove electronic ballot question title from ech vote mapping

### 🔄 Changed

- refactor dockerfile and reduce cache layers

### 🔒 Security

- introduce user id and group id to avoid random assignment
- use exec form to avoid shell interpretation

### 🔄 Changed

- ensure candidate numbers have two digits in eCH exports

### 🆕 Added

- Injections for PoliticalAssemblyStateSetPastJob and PoliticalAssemblyStateArchiveJob. Activation of event generation for past lock and archive of political assemblies.

### 🔄 Changed

- initiative number of members committee is required for all dois

### 🆕 Added

- add Ech0157v5 and Ech0159v5

### 🆕 Added

- add e-collecting referendum and initiative properties

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
