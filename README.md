# Uno/UX core

[![AppVeyor build status](https://img.shields.io/appveyor/ci/fusetools/uno/master.svg?logo=appveyor&logoColor=silver&style=flat-square)](https://ci.appveyor.com/project/fusetools/uno/branch/master)
[![Travis CI build status](https://img.shields.io/travis/fuse-open/uno/master.svg?style=flat-square)](https://travis-ci.org/fuse-open/uno)
[![NPM package](https://img.shields.io/npm/v/@fuse-open/uno.svg?style=flat-square)](https://www.npmjs.com/package/@fuse-open/uno)
[![License: MIT](https://img.shields.io/github/license/fuse-open/uno.svg?style=flat-square)](LICENSE.txt)
[![Slack](https://img.shields.io/badge/chat-on%20slack-blue.svg?style=flat-square)](https://slackcommunity.fusetools.com/)

Welcome to Uno, the core component in [Fuse Open], a native app development tool suite.

## Install

```
npm install @fuse-open/uno
```

This will install `uno`. Pass `--save-dev` to install as a dependency in your local project, or `-g` to install
the command globally on your system.

Please note that this package contains .NET software that will need [Mono](http://www.mono-project.com/download/)
to run on Linux and macOS.

## Abstract

We're here to help [Fuse Open] development by building and maintaining several related pieces of core technology.

* Cross-platform tools for building and running applications
* Core libraries and platform abstraction
* Uno programming language and compiler
* Uno project format and build engine
* UX markup language and compiler
* Uno/UX test runner

Uno is used on Linux, macOS and Windows, and makes native apps for the following platforms:

* Android
* iOS
* Linux (native)
* macOS (native or Mono)
* Windows (native or .NET)

[Fuse Open]: https://fuseopen.com/

### Uno syntax

```uno
class App : Uno.Application
{
    public App()
    {
        debug_log "Hello, world!";
    }
}
```

The Uno programming language is a fast, native dialect of [C#] that can cross-compile to *any native platform* (in theory),
by emitting portable [C++11] for mobile or desktop platforms, or [CIL bytecode] for desktop platforms (Mono/.NET) —
designed for developing high-performance UI-engines, platform abstractions or integrations, and other kinds of
software traditionally required written in native C/C++.

Access all APIs and features on the target platforms directly in Uno — add a snippet of *foreign code*, and
our compiler automatically generates the glue necessary to interoperate (two-way) with a foreign language.
The following foreign languages are supported:

* [C++11], [C99]
* [Java] (Android)
* [Objective-C] (iOS, macOS)
* [Swift] (iOS)

[C#]: https://en.wikipedia.org/wiki/C_Sharp_(programming_language)
[C++11]: https://en.wikipedia.org/wiki/C++11
[C99]: https://en.wikipedia.org/wiki/C99
[CIL bytecode]: https://en.wikipedia.org/wiki/Common_Intermediate_Language
[Java]: https://en.wikipedia.org/wiki/Java_(programming_language)
[Objective-C]: https://en.wikipedia.org/wiki/Objective-C
[Swift]: https://en.wikipedia.org/wiki/Swift_(programming_language)

### Run-time features

* Memory in Uno is managed *semi-automatically* by [automatic reference counting], avoiding unpredictable GC stalls.
* *Real* [generics] – sharing the same compiled code in all generic type instantiations, without [boxing] values, and with
  *full run-time type system* support – avoiding exploding code-size and compile-times (while still being fast).
* *(Opt-in)* [reflection] on *all* platforms – to dynamically create objects and invoke methods based on type information
  *only known at run-time* – enabling high-level Fuse features such as *live-previewing UX documents*.

[automatic reference counting]: https://en.wikipedia.org/wiki/Automatic_Reference_Counting
[boxing]: https://en.wikipedia.org/wiki/Object_type_(object-oriented_programming)#Boxing
[generics]: https://en.wikipedia.org/wiki/Generic_programming
[reflection]: https://en.wikipedia.org/wiki/Reflection_(computer_programming)

See https://fuseopen.com/docs/ for more information about the Uno/UX (and JavaScript) stack.

## Build instructions

Please read [the build instructions](docs/build-instructions.md) for details
on how to build the source code.

## Contributing

Please read [CONTRIBUTING](CONTRIBUTING.md) for details on our code of
conduct, and the process for submitting pull requests to us.

### Reporting issues

Please report issues [here](https://github.com/fuse-open/uno/issues).

## Configuration

Please read [the configuration reference documentation][doc1] for details on how to
set up uno's configuration files for your build-environment.

## Command Line Reference

Please read [the command-line reference documentation][doc2] for details on how to
use uno's command-line interface.

[doc1]: docs/configuration.md
[doc2]: docs/command-line-reference.md
