pluginManagement { repositories { google(); mavenCentral(); gradlePluginPortal() } }
dependencyResolutionManagement { repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS); repositories { google(); mavenCentral() } }
rootProject.name = "CampusCarpoolMobile"
include(":core")
if (!providers.gradleProperty("coreOnly").isPresent) include(":app")
