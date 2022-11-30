# ✨ Changelog (`v1.42.2`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Version Info

```text
This version -------- v1.42.2
Previous version ---- v1.27.15
Initial version ----- v1.27.15
Total commits ------- 260
```

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

The readmodel needs to be recreated after this MR.

## [v1.27.16] - 2022-05-23

### 🔄 Changed

- code quality issues

## [v1.27.15] - 2022-05-18

### 🎉 Initial release for Bug Bounty
