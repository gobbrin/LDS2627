# I094 — análise estática Android

## Configuração
ktlint 1.5.0; Android Lint com warningsAsErrors; verificação JaCoCo de pelo menos 80% de linhas em core e app. Os limites e exclusões funcionais foram preservados.

## Reprodução
Na pasta frontend-mobile, executar com JDK 21 e SDK 36:

`./gradlew :core:check :app:ktlintCheck :app:verificarCoberturaApp :app:lintDebug :app:assembleDebug :app:assembleDebugAndroidTest`

## Resultados locais observados
Os comandos acima passaram no conjunto preparado. Testes: 90 no core e 145 na app, sem falhas nem testes saltados. Cobertura de linhas: core 439/478 = 91,84%; app 2079/2461 = 84,48%. O APK debug e o APK instrumentado de testes foram compilados. Não foi feita execução em dispositivo; a suite de sistema completa continua por concluir.

Foram corrigidos acessos a StateFlow dentro de composição, edições de preferências, construção de ViewModels nos testes e formatação. Os limites do cliente foram alinhados com o backend: mensagens de 1 a 1000 caracteres e avisos de 1 a 100.

## Análise e limites
A API adicional usa os interceptores existentes. As datas usam Europe/Lisbon, a aceitação requer a simulação do mesmo pedido e os testes verificam erros sem perda de texto. Ainda é necessário validar ciclo de vida, acessibilidade, autorização do servidor, mapas e FCM no ambiente integrado. O teste local não demonstra que os critérios de sistema/aceitação estejam todos satisfeitos.

## Fecho
Repetir os comandos na branch final e guardar logs, versões e evidências associados ao mesmo commit. O responsável conclui o relatório; não existe aprovação de revisão ou auditoria neste documento.
