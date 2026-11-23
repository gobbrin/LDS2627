# Pipeline do sistema completo — I096

## Gatilhos e fluxo
`sistema-ci.yml` executa em pull requests, push para develop/main e por iniciativa manual. Reutiliza backend-ci.yml, frontend-web-ci.yml e frontend-mobile-ci.yml. O gate global só passa se os três jobs concluírem com sucesso; erro, cancelamento ou job saltado não contam como aprovação. Execuções mais antigas da mesma referência são canceladas. A permissão global é contents: read.

## Componentes
- Backend: .NET definido em global.json; restore, build Release, format, pacotes vulneráveis, testes, SQL Server real, cobertura mínima e artefactos.
- Web: Node 24, npm ci com lockfile, Prettier, ESLint, Karma/Chrome com cobertura mínima e build Angular. O DOM simulado é apenas uma verificação suplementar local.
- Mobile: Java 21, Gradle wrapper, SDK 36, ktlint, Android Lint, testes e cobertura JaCoCo, APK debug e compilação dos testes instrumentados. Compilar testes instrumentados não significa executá-los num dispositivo.

## Qualidade e evidências
Conservar os gates de 80% de linhas configurados por componente. Não ignorar falhas para desbloquear merges. Os uploads com always() recolhem relatórios também quando um gate falha; isso não é aprovação. A I101 reúne as evidências das execuções reais e a I097/I098 os resultados de testes.

## Publicação
Este incremento prepara integração e artefactos. A publicação do sistema completo é a I102 e depende dos resultados/aceitação e das credenciais do ambiente autorizado. Não existe deployment externo comprovado neste pacote. Preparar secrets nos ambientes do repositório apenas quando o destino estiver definido; nunca colocá-los no código, ZIP ou logs. Uma versão candidata deve corresponder à mesma revisão que passou o gate global e a aceitação.

## Reproduzir e diagnosticar
Executar os comandos do workflow a partir de backend/, frontend-web/ e frontend-mobile/. Em falha, abrir primeiro o job que falhou, ler o erro original, corrigir e repetir os checks afetados. Confirmar versões, artefactos e correspondência ao PR integrado. Não tratar um run anterior como evidência de código alterado depois.

## Pendências de integração
I111/I112: contrato comum, rota, simulação, avaliação e sessão/SignalR. Firebase: configuração do grupo e testes reais. Automação I087/I092: completar todos os cenários especificados e verificar os gates; o pacote atual inclui bases parciais e não uma declaração de cobertura integral.
