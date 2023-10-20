# ✨ Changelog (`v1.57.1`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v1.57.1
Previous version ---- v1.51.0
Initial version ----- v1.27.15
Total commits ------- 27
```

## [v1.57.1] - 2023-10-05

### 🔄 Changed

- Update lib dependency

## [v1.57.0] - 2023-09-01

### 🔄 Changed

- political first name of candidate from simple text to complex text

## [v1.56.3] - 2023-08-29

### 🔄 Changed

- remove list unions when a main list is removed

## [v1.56.2] - 2023-08-29

### 🔄 Changed

- remove empty list unions when a list is removed

## [v1.56.1] - 2023-08-29

### 🔄 Changed

- remove party from candidate text for proportional election

## [v1.56.0] - 2023-08-28

### 🆕 Added

- add health check for cert pinned authorities

## [v1.55.2] - 2023-08-28

### 🔄 Changed

- extend ech-0157 import to map all languages for occupation.

## [v1.55.1] - 2023-08-28

### 🔄 Changed

- extend eCH-0157 import to add multi-language support for `occupationTitle` candidate attribute
- extend eCH-0157 export to add multi-language support for empty list description `WoP`
- extend candidate text transformer to differentiate between custom templates

## [v1.55.0] - 2023-08-22

### 🔄 Changed

- Update eai and lib dependency to deterministic version

## [v1.54.3] - 2023-08-18

### ❌ Removed

- remove swiss post order number

## [v1.54.2] - 2023-08-16

### 🔄 Changed

- list import delete list only on same order number

## [v1.54.1] - 2023-08-10

### 🔄 Changed

- map ballot title occupation from candidate lookup extension during eCH-0157 import

## [v1.54.0] - 2023-08-10

### 🔄 Changed

- import party for proportional election candidates

## [v1.53.0] - 2023-07-26

### 🆕 Added

- add swiss post data

## [v1.52.2] - 2023-07-26

### 🔄 Changed

- Migrate optional owned domain of influence print data correctly

## [v1.52.1] - 2023-07-18

### 🆕 Added

- Add domain of influence voting card shipping choice

## [v1.52.0] - 2023-07-12

### 🆕 Added

- integrate malware scanner to check logo and ech-files import

## [v1.51.8] - 2023-06-26

### 🔄 Changed

- export vote sorting by domain of influence type

## [v1.51.7] - 2023-06-23

### 🔄 Changed

- first import all root list unions

## [v1.51.6] - 2023-06-23

### 🔄 Changed

- Sort contests depending on states

## [v1.51.5] - 2023-06-21

### 🆕 Added

- add export vote description for all languages

## [v1.51.4] - 2023-06-20

### 🔄 Changed

- change candidate text for proportional election

## [v1.51.3] - 2023-06-20

### 🔄 Changed

- list order of precendence optional for import

## [v1.51.2] - 2023-06-20

### 🔄 Changed

- correctly import proportional elections from eCH-0157

## [v1.51.1] - 2023-06-13

### 🔄 Changed

- extend complex input validation rule

## [v1.51.0] - 2023-05-25

### 🔄 Changed

- overwrite existing lists on eCH-0157 list import

## [v1.50.1] - 2023-05-02

### 🔄 Changed

- update cd-templates to resolve blocking deploy-trigger

## [v1.50.0] - 2023-05-01

### 🔄 Changed

- add domain of influence sap customer order number

## [v1.49.6] - 2023-05-01

### 🆕 Added

- add domain of influence sap customer order number

## [v1.49.5] - 2023-04-17

### 🔄 Changed

- set canton on doi's after update correctly

## [v1.49.4] - 2023-04-05

### 🆕 Added

- add party and incumbent to candidate text

## [v1.49.3] - 2023-04-03

### ❌ Removed

- remove incumbentYesNo field for eCH election export

## [v1.49.2] - 2023-03-27

### 🔄 Changed

- always unset canton for non-root doi's

## [v1.49.1] - 2023-03-24

### 🔄 Changed

- Use latest CI templates

## [v1.49.0] - 2023-03-13

### 🔄 Changed

- Store Canton in Domain Of Influence Read Model

## [v1.48.1] - 2023-03-10

### 🔄 Changed

- update lib to fix eCH issues
- export eCH ballot question ID correctly

## [v1.48.0] - 2023-02-24

### 🔄 Changed

- raise voting lib version including an update to allow additional characters "«»;& for complex text input validation
- implement new mocked clock member

## [v1.47.0] - 2023-02-20

### 🔄 Changed

- add wabstic wmwahlergebnis report

## [v1.46.0] - 2023-02-01

### 🔄 Changed

- set bfs required and unique for doi's of type MU

## [v1.45.7] - 2023-01-27

### 🔄 Changed

- ensure valid language on eCH import

## [v1.45.6] - 2023-01-24

### 🔄 Changed

- map eCH dates correctly to UTC

## [v1.45.5] - 2023-01-19

### 🔄 Changed

- fix eCH import event signature timing issue and validate eCH imports

### ❌ Removed

- remove internal description, invalid votes and individual empty ballots allowed from elections

### 🆕 Added

- add domain of influence canton

### 🔄 Changed

- update library to extend complex text input validation rules with dash sign

### 🆕 Added

- add candidate origin

### 🆕 Added

- add request recorder tooling for load testing playbook

### 🔄 Changed

- disable proxy in launch settings for local development

### 🔄 Changed

- update voting lib to add transient subscription health check

### 🔄 Changed

- Moved PKCS11 device health check to the publisher

### 🔒 Security

- Set the valid to on the event signature public key correctly

### 🔒 Security

- Added event signature

### 🔄 Changed

- move default object storage initialization to specific appsettings

### 🆕 Added

- input validation

### 🆕 Added

- add log messages for debugging within the updated voting lib

### 🔄 Changed

- use unique identifier for messaging consumer endpoints so each horizontally scaled instance consumes change notifications
- update rabbitmq image for local development

### 🆕 Added

- Name for protocol for domain of influence and counting circle
- Sortnumber for counting circle
- Protocol sort types for domain of influence and counting circle

### 🔄 Changed

- serialize VOTING votes as eCH ballots and vice versa for deserialization

### 🔄 Changed

- Don't store domain of influence name in logo storage, as it may contain non-ASCII characters

### 🆕 Added

- Add eCH message type to eCH-exports

### 🔄 Changed

- set default value of review procedure

### 🆕 Added

- added review procedure and enforce for counting circle property for vote, majority election and proportional election

### 🆕 Added

- add Serilog.Expressions to exclude status endpoints from serilog request logging on success only

### 🆕 Added

- add application builder extension which is adding the serilog request logging middleware enriching the log context with tracability properties

### 🔄 Changed

- exchanged custom health check with ef core default one

### 🔄 Changed

- Restrict end of testing phase with a max timespan before the contest date

### 🔄 Changed

- Allow modification of political business number of secondary majority election after testing phase has ended

### 🔄 Changed

- exchanged ef core default health check with custom one

### 🔄 Changed

- Updated dependencies

### 🔄 Changed

- Restricted modification of code property of counting circles to admins

### 🔒 Security

- Restricted modification of code property of counting circles to the admin role

### 🆕 Added

- Events to notify political businesses and political business unions about a contest merge

### 🔄 Changed

- logo url can be loaded with doi read permissions

### 🔄 Changed

- rewrite comments & validations
- updated lib version

### 🔄 Changed

- Restrict read permission of political businesses and dependencies (eg. candidates or ballot groups) to the responsible tenant
- Restrict export permissions of political businesses to the responsible tenant and tenants higher up in the hierarchy

### 🔒 Security

- Restrict read permission of political businesses and dependencies (eg. candidates or ballot groups) to the responsible tenant
- Restrict export permissions of political businesses to the responsible tenant and tenants higher up in the hierarchy

### 🆕 Added

- add domain of influence external printing center eai message type

### 🆕 Added

- CORS configuration support

### 🆕 Added

- added voting documents e-voting message type to canton settings

### 🔄 Changed

- upgraded underlying dotnet image to sdk 6.0.301 after gituhb issue [#24269](https://github.com/dotnet/sdk/issues/24269) has been fixed

### 🔄 Changed

- added OpenAPI description

### 🔄 Changed

- Restrict counting circle updates for users in role ElectionAdmin to the authorised tenant
- Restrict domain of influence updates for users in role ElectionAdmin to the authorised tenant

### 🔒 Security

- Restrict counting circle updates for users in role ElectionAdmin to the authorised tenant
- Restrict domain of influence updates for users in role ElectionAdmin to the authorised tenant

### 🔒 Security

- Fixed authorization check for importing majority election candidates and proportional election lists

### 🆕 Added

- add query split behavior where needed

### 🔄 Changed

- extend evoting date with time

The readmodel needs to be recreated after this MR.

## [v1.45.4] - 2023-01-18

### 🔄 Changed

- correct eCH-0157 export

## [v1.45.3] - 2023-01-11

### 🔄 Changed

- export and import eCH list unions correctly

## [v1.45.2] - 2023-01-09

### 🔄 Changed

- fix eCH import event signature timing issue and validate eCH imports

## [v1.45.1] - 2023-01-04

### ❌ Removed

- remove internal description, invalid votes and individual empty ballots allowed from elections

## [v1.45.0] - 2022-12-22

### 🔄 Changed

- add export provider

## [v1.44.2] - 2022-12-16

### 🆕 Added

- add domain of influence canton

## [v1.44.1] - 2022-12-14

### 🔄 Changed

- update library to extend complex text input validation rules with dash sign

## [v1.44.0] - 2022-12-05

### 🆕 Added

- add candidate origin

## [v1.43.0] - 2022-12-02

### 🆕 Added

- add request recorder tooling for load testing playbook

### 🔄 Changed

- disable proxy in launch settings for local development

## [v1.42.2] - 2022-11-30

### 🔄 Changed

- update voting lib to add transient subscription health check

## [v1.42.1] - 2022-11-29

### 🔄 Changed

- Moved PKCS11 device health check to the publisher

### 🔒 Security

- Set the valid to on the event signature public key correctly

## [v1.42.0] - 2022-11-29

### 🔄 Changed

- restrict logo upload to PNG and SVG

## [v1.41.0] - 2022-11-29

### 🔒 Security

- Added event signature

## [v1.40.1] - 2022-11-29

### 🔄 Changed

- move default object storage initialization to specific appsettings

## [v1.40.0] - 2022-11-28

### 🆕 Added

- input validation

## [v1.39.0] - 2022-11-17

### 🔄 Changed

- add new admin management services

## [v1.38.5] - 2022-11-10

### 🔄 Changed

- eCH corrections

## [v1.38.4] - 2022-11-07

### 🆕 Added

- add log messages for debugging within the updated voting lib

### 🔄 Changed

- use unique identifier for messaging consumer endpoints so each horizontally scaled instance consumes change notifications
- update rabbitmq image for local development

## [v1.38.3] - 2022-10-25

### 🔄 Changed

- correct export of eCH candidates

## [v1.38.2] - 2022-10-24

### 🔄 Changed

- correctly export eCH entities

## [v1.38.1] - 2022-10-24

### 🔄 Changed

- correct eCH-0157 and eCH-0159 definitions

## [v1.38.0] - 2022-10-10

### 🆕 Added

- Name for protocol for domain of influence and counting circle
- Sortnumber for counting circle
- Protocol sort types for domain of influence and counting circle

## [v1.37.0] - 2022-10-05

### 🔄 Changed

- serialize VOTING votes as eCH ballots and vice versa for deserialization

## [v1.36.1] - 2022-09-27

### 🔄 Changed

- Don't store domain of influence name in logo storage, as it may contain non-ASCII characters

## [v1.36.0] - 2022-09-23

### 🆕 Added

- Add eCH message type to eCH-exports

## [v1.35.1] - 2022-09-15

### 🔄 Changed

- set default value of review procedure

## [v1.35.0] - 2022-09-13

### 🆕 Added

- added review procedure and enforce for counting circle property for vote, majority election and proportional election

## [v1.34.0] - 2022-09-06

### 🆕 Added

- add Serilog.Expressions to exclude status endpoints from serilog request logging on success only

## [v1.33.0] - 2022-09-05

### 🆕 Added

- add application builder extension which is adding the serilog request logging middleware enriching the log context with tracability properties

## [v1.32.11] - 2022-09-05

### 🔄 Changed

- exchanged custom health check with ef core default one

## [v1.32.10] - 2022-08-31

### 🔄 Changed

- Restrict end of testing phase with a max timespan before the contest date

## [v1.32.9] - 2022-08-30

### 🔄 Changed

- Allow modification of political business number of secondary majority election after testing phase has ended

## [v1.32.8] - 2022-08-25

### 🔄 Changed

- exchanged ef core default health check with custom one

## [v1.32.7] - 2022-08-25

### 🔄 Changed

- Updated dependencies

## [v1.32.6] - 2022-08-24

### 🔄 Changed

- Restricted modification of code property of counting circles to admins

### 🔒 Security

- Restricted modification of code property of counting circles to the admin role

## [v1.32.5] - 2022-08-23

### 🔄 Changed

- refactoring and clean up code smells
- local certificate pinning

## [v1.32.4] - 2022-08-15

### 🆕 Added

- Events to notify political businesses and political business unions about a contest merge

## [v1.32.3] - 2022-07-29

### 🔄 Changed

- logo url can be loaded with doi read permissions

## [v1.32.2] - 2022-07-26

### 🔄 Changed

- rewrite comments & validations
- updated lib version

## [v1.32.1] - 2022-07-25

### 🔄 Changed

- Restrict read permission of political businesses and dependencies (eg. candidates or ballot groups) to the responsible tenant
- Restrict export permissions of political businesses to the responsible tenant and tenants higher up in the hierarchy

### 🔒 Security

- Restrict read permission of political businesses and dependencies (eg. candidates or ballot groups) to the responsible tenant
- Restrict export permissions of political businesses to the responsible tenant and tenants higher up in the hierarchy

## [v1.32.0] - 2022-07-15

### 🆕 Added

- add domain of influence external printing center eai message type

## [v1.31.0] - 2022-07-13

### 🆕 Added

- CORS configuration support

## [v1.30.0] - 2022-07-06

### 🆕 Added

- added voting documents e-voting message type to canton settings

## [v1.29.0] - 2022-06-27

### 🔄 Changed

- upgraded underlying dotnet image to sdk 6.0.301 after gituhb issue [#24269](https://github.com/dotnet/sdk/issues/24269) has been fixed

## [v1.28.5] - 2022-06-23

### 🔄 Changed

- added OpenAPI description

## [v1.28.4] - 2022-06-21

### 🔄 Changed

- Restrict counting circle updates for users in role ElectionAdmin to the authorised tenant
- Restrict domain of influence updates for users in role ElectionAdmin to the authorised tenant

### 🔒 Security

- Restrict counting circle updates for users in role ElectionAdmin to the authorised tenant
- Restrict domain of influence updates for users in role ElectionAdmin to the authorised tenant

## [v1.28.3] - 2022-06-21

### 🔒 Security

- Fixed authorization check for importing majority election candidates and proportional election lists

## [v1.28.2] - 2022-06-13

### 🆕 Added

- add query split behavior where needed

## [v1.28.1] - 2022-06-10

### 🔄 Changed

- use new ssl cert option instead of preprocessor directive

## [v1.28.0] - 2022-06-02

### 🔄 Changed

- generate dotnet swagger docs

## [v1.27.19] - 2022-05-25

### 🔄 Changed

- extend evoting date with time

## [v1.27.18] - 2022-05-24

### 🔄 Changed

- contest merger merge simple political businesses

## [v1.27.17] - 2022-05-23

### 🔄 Changed

- lib version

## [v1.27.16] - 2022-05-23

### 🔄 Changed

- code quality issues

## [v1.27.15] - 2022-05-18

### 🎉 Initial release for Bug Bounty
