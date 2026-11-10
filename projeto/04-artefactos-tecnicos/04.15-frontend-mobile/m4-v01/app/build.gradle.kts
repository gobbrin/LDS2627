import java.net.URI
import org.jetbrains.kotlin.gradle.dsl.JvmTarget

plugins {
    id("com.android.application")
    kotlin("android")
    id("org.jetbrains.kotlin.plugin.compose")
    id("org.jlleitschuh.gradle.ktlint")
    jacoco
}
val apiUrl = providers.gradleProperty(
    "campusApiUrl"
).orElse(providers.environmentVariable("CAMPUS_API_URL"))
    .orElse(
        "https://campuscarpool-api-g15-ase2b6g6cbabf7d7.germanywestcentral-01.azurewebsites.net/api/v1/"
    )
val endpoint = URI(apiUrl.get())
require(
    endpoint.scheme in listOf("https", "http") &&
        endpoint.userInfo == null &&
        endpoint.query == null &&
        endpoint.fragment == null &&
        endpoint.path == "/api/v1/"
) {
    "campusApiUrl deve terminar em /api/v1/ e não conter credenciais."
}
val hostsLocais = setOf("10.0.2.2", "127.0.0.1", "localhost")
require(endpoint.scheme == "https" || endpoint.host in hostsLocais) {
    "campusApiUrl só pode usar HTTP com 10.0.2.2, 127.0.0.1 ou localhost."
}
android {
    namespace = "pt.ipca.g15.campuscarpool"
    compileSdk = 36
    defaultConfig {
        applicationId = "pt.ipca.g15.campuscarpool"
        minSdk = 29
        targetSdk = 36
        versionCode = 1
        versionName = "0.1.0"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        buildConfigField("String", "API_BASE_URL", "\"${apiUrl.get()}\"")
    }
    buildFeatures {
        compose = true
        buildConfig = true
    }
    buildTypes {
        debug {
            enableAndroidTestCoverage = true
            enableUnitTestCoverage = true
        }
        release { isMinifyEnabled = false }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility =
            JavaVersion.VERSION_17
    }
    lint {
        disable += "OldTargetApi"
        abortOnError = true
        warningsAsErrors = true
        // Dependem da data e da rede: uma versão nova publicada lá fora partiria a pipeline
        // sem qualquer alteração no projeto. As versões são fixas neste ficheiro.
        disable += setOf("GradleDependency", "NewerVersionAvailable")
    }
    testOptions { unitTests.isIncludeAndroidResources = true }
}
kotlin { compilerOptions { jvmTarget.set(JvmTarget.JVM_17) } }

jacoco { toolVersion = "0.8.13" }
tasks.withType<Test>().configureEach {
    // O Robolectric carrega as classes num classloader próprio; sem isto a cobertura fica vazia.
    extensions.configure<JacocoTaskExtension> {
        isIncludeNoLocationClasses = true
        excludes = listOf("jdk.internal.*")
    }
}
val classesApp = layout.buildDirectory.dir("tmp/kotlin-classes/debug").map { pasta ->
    fileTree(pasta) { exclude("**/R.class", "**/R$*.class", "**/BuildConfig.class") }
}
val execucaoTestes =
    layout.buildDirectory.file(
        "outputs/unit_test_code_coverage/debugUnitTest/testDebugUnitTest.exec"
    )
val jacocoApp by tasks.registering(JacocoReport::class) {
    dependsOn("testDebugUnitTest")
    classDirectories.setFrom(classesApp)
    sourceDirectories.setFrom("src/main/kotlin")
    executionData.setFrom(execucaoTestes)
    reports {
        xml.required.set(true)
        html.required.set(true)
    }
}
val verificarCoberturaApp by tasks.registering(JacocoCoverageVerification::class) {
    dependsOn("testDebugUnitTest")
    classDirectories.setFrom(classesApp)
    sourceDirectories.setFrom("src/main/kotlin")
    executionData.setFrom(execucaoTestes)
    violationRules {
        rule {
            limit {
                counter = "LINE"
                minimum = "0.80".toBigDecimal()
            }
        }
    }
    finalizedBy(jacocoApp)
}
tasks.named("check") { dependsOn(verificarCoberturaApp) }

val verificarHttpsRelease by tasks.registering {
    doLast { require(endpoint.scheme == "https") { "A compilação release exige HTTPS." } }
}
tasks.matching { it.name == "preReleaseBuild" }.configureEach { dependsOn(verificarHttpsRelease) }
dependencies {
    androidTestImplementation("androidx.test.espresso:espresso-core:3.7.0")
    implementation(project(":core"))
    implementation(platform("androidx.compose:compose-bom:2025.10.01"))
    implementation("androidx.activity:activity-compose:1.11.0")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.lifecycle:lifecycle-runtime-compose:2.9.4")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.9.4")
    implementation("androidx.navigation:navigation-compose:2.9.5")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.10.2")
    implementation("com.google.code.gson:gson:2.13.2")
    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")
    testImplementation(platform("androidx.compose:compose-bom:2025.10.01"))
    testImplementation("androidx.compose.ui:ui-test-junit4")
    testImplementation("androidx.test:core:1.7.0")
    testImplementation("junit:junit:4.13.2")
    testImplementation("org.jetbrains.kotlinx:kotlinx-coroutines-test:1.10.2")
    testImplementation("org.robolectric:robolectric:4.16")
    androidTestImplementation(platform("androidx.compose:compose-bom:2025.10.01"))
    androidTestImplementation("androidx.compose.ui:ui-test-junit4")
    androidTestImplementation("androidx.test.ext:junit:1.3.0")
    androidTestImplementation("androidx.test:core:1.7.0")
    androidTestImplementation("androidx.test:runner:1.7.0")
}

ktlint { version.set("1.5.0") }
