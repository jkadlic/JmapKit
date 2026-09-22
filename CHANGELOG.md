# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

- Session discovery, bearer token authentication, and typed JMAP method support
  (`get`, `set`, `changes`, `copy`, `query`, `queryChanges`, `echo`) per RFC 8620.
- `IJmapObject.SupportedMethods`, declaring which JMAP methods a data type supports per
  RFC 8620 §2. `JmapMethodInvocation.Create<T>` validates against it and throws
  `JmapUnsupportedMethodException` for an unsupported method, so an invalid call fails before
  any request is sent.