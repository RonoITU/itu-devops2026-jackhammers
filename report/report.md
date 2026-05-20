# ITU-MiniTwit — BSc DevOps, Software Evolution and Software Maintenance

**Group:** `BSc_group_m`
**Repository:** `https://github.com/RonoITU/itu-devops2026-jackhammers`
**Issue tracker:** `https://github.com/RonoITU/itu-devops2026-jackhammers/issues`
**Monitoring dashboard:** `http://178.104.27.224:3000/d/chirp-aspnet-001/windysquirrels-monitoring-dashboard`
**Logging dashboard:** `http://178.104.27.224:3000/d/app-logging-dashboard/windysquirrels-logging-dashboard`

| Name | ITU ID |
|------|--------|
| Christian Philip Jørgensen | chpj@itu.dk |
| Jakob Sønder | jakso@itu.dk |
| Jacob Sponholtz | spon@itu.dk |
| Ronas Jacob Coban Olsen | rono@itu.dk |
| Rasmus Alexander Christiansen | ralc@itu.dk |

---

## 1. System's Perspective

### 1.1 Design and Architecture
*Author(s): *
<!-- Describe and illustrate the overall design and architecture of your ITU-MiniTwit system.
     Include a diagram (e.g. a component or deployment diagram) stored in report/images/.
     Example: ![Architecture Diagram](images/architecture.png) -->

### 1.2 Dependencies
*Author(s): *
<!-- List and briefly describe all technologies, frameworks, libraries, and tools your system
     depends on at all levels of abstraction (runtime, build, infrastructure, CI/CD, etc.).
     Example table: -->

| Technology / Tool | Version | Purpose |
|-------------------|---------|---------|
| <!-- e.g. Docker --> | <!-- 26.x --> | <!-- Containerisation --> |
| <!-- e.g. PostgreSQL --> | <!-- 16 --> | <!-- Relational database --> |
| <!-- ... --> | | |

### 1.3 Current State of the System
*Author(s): *
<!-- Describe the current state of the system. Include results from static analysis tools
     (e.g. SonarQube, golangci-lint, ESLint) and any quality assessments you have run.
     Reference specific metrics or screenshots where relevant. -->

---

## 2. Process' Perspective

### 2.1 CI/CD Pipeline
*Author(s): Ronas Olsen and Jacob Sponholtz

<!-- Describe and illustrate all stages and tools in your CI/CD pipeline, including how
     code is built, tested, and deployed/released to production.
     Include a diagram if helpful, e.g.: ![CI/CD Pipeline](images/cicd_pipeline.png) -->

![Diagram of the CI/CD Pipeline (excalidraw.com)](images/ci-cd-diagram.png)

The source code is hosted on GitHub.
We follow a modified GitFlow branching strategy: Feature development on a `develop` branch and feature branches, but without the use of release branches.
Instead `develop` is merged directly to the main branch on release, and all QA is handled as part of the CI of features to the  `develop` branch. 

The main branch is the version currently in production.
Versions are also tagged semi-automatically by the CD workflow with each new release. 

Any contributor can clone the repository, create a new feature branch, and work using their preferred tools.
Simple instructions are provided in the README on how to build, run and test locally.
To integrate a feature into the next release, the contributor opens a pull request towards `develop`.
This triggers the CI GitHub Action to run our testing suite (Unit, Integration, E2E) and static analysis tools (SonarCube, Codacy) so that we will have concrete QA evidence alongside the changes to review. 

The contributor will also get immediate feedback from the analysis tool, indicating any new issues and the test coverage on new code.
Once the contributor is happy with the new feature(s), we ask for the feature branch to be merged.

To deploy a new release, the developer must first assert the new commits on `develop`, increment the VERSION file acordningly, and open a pull-request towards main.
This pull-request first triggers the CI GitHub Action. When this action has been completed succesfully, the developer can merge towards main.
The merge triggers the CD actions, in which a new Docker image is built and uploaded to Docker Hub.
Github then connects to the server via ssh to recreate and restart the containers, using the new release image.

Finally the CD action will tag the release with version number, and create a new release on GitHub.

### 2.2 Monitoring
*Author(s): *

<!-- Describe how you monitor your system and what precisely you monitor
     (metrics, alerts, dashboards, tools used — e.g. Prometheus, Grafana). -->

### 2.3 Logging
*Author(s): *

<!-- Describe what you log in your system, how logs are collected, aggregated,
     and queried (e.g. ELK stack, Loki/Grafana, Fluentd). -->

### 2.4 Security Hardening
*Author(s): *

<!-- Briefly describe the measures taken to security-harden your system
     (e.g. secrets management, network policies, dependency scanning, HTTPS, least-privilege). -->

### 2.5 Availability and Scaling
*Author(s): *

<!-- Describe how you handle availability and scaling
     (e.g. load balancing, horizontal scaling, health checks, rolling deployments). -->

---

## 3. Reflection Perspective

### 3.1 Evolution and Refactoring
*Author(s): *

<!-- Describe the biggest challenges encountered when evolving and refactoring the system.
     How were they solved? Link to relevant commits, issues, or PRs. -->

### 3.2 Operation
*Author(s): *

<!-- Describe the biggest operational challenges and how they were resolved.
     Link to relevant incidents, runbooks, or monitoring alerts. -->

### 3.3 Maintenance
*Author(s): *

<!-- Describe challenges related to maintaining the system over the term
     (dependency updates, technical debt, documentation, etc.).
     Link to relevant issues or commits. -->

### 3.4 DevOps Reflection
*Author(s): *

<!-- Reflect on the "DevOps" style of your work. What did you do differently compared to
     previous development projects? What worked well and what did not? -->

---

## 4. Use of Generative AI
*Author(s):* Rasmus

**GitHub Copilot** was used to assist with boilerplate code generation throughout development. It provided inline auto-completion that often matched the intended behavior being implemented, and helped untangle unfamiliar functionality across the broader codebase. This sped up the coding process significantly, particularly for repetitive or structurally predictable code.

**Microsoft Copilot** was used for general consultation when extending the infrastructure, serving as a quick reference for exploring options and approaches before committing to a direction.

**Claude** was used for more in-depth technical discussions, typically involving specific code examples. It helped provide clarity on complex topics and was particularly useful when a concept required more thorough explanation than a quick search could offer.

<!-- Reflection if word count allows: -->
 
<!-- Did the tools speed up your work? Did they introduce errors or bad
     practices that you had to fix? What would you do differently? -->