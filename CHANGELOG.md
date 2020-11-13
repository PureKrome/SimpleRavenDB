# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]


## [2.0.0-RC.1] - 2020-11-14
### Fixed 
- CheckRavenDbPolicy -> CheckRavenDbPolicyAsync (sync to async .. was ignoring exceptions getting thrown)
- SetupRavenDbPolicyAsync -> SetupRavenDbPolicyAsync (sync to async .. was ignoring exceptions getting thrown)

### Added
- Added Changelog.
- Multi-Targetting NetStandard 2.0 and NET5.0

## [1.0.0] - 2020-11-03
### Added
- Initial release.
