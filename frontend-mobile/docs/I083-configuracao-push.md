# Configuração de push — I083

1. Criar/usar a aplicação Android `pt.ipca.g15.campuscarpool` no projeto Firebase autorizado do grupo.
2. Gerar recursos Android `google_app_id`, `gcm_defaultSenderId`, `google_api_key` e `project_id` a partir do google-services.json desse projeto (ou aplicar o plugin oficial Google Services). Não incluir contas de serviço nem chaves privadas no frontend.
3. Configurar o mesmo projeto no backend. Só PedidoRecebido/PedidoAceite/PedidoRejeitado são elegíveis.
4. No dispositivo, abrir Notificações e ativar push; conceder permissão Android 13+. Testar permissão negada/revogada, logout, expiração, troca de conta, renovação do token e aplicação em segundo plano.
5. O backend atual envia `notification` + `data`. O Android pode apresentar o bloco notification diretamente em segundo plano, fora do filtro onMessageReceived. É necessário confirmar com I112 a revogação do token e preferir dados sem bloco notification para garantir filtragem e sessão antes da apresentação. Não aprovar este critério sem teste real.

Firebase não configurado: a caixa HTTP e o SignalR mantêm-se utilizáveis; push não se considera validado. Sem configuração ou dispositivo não existem evidências FCM neste pacote.

Fontes técnicas: https://firebase.google.com/docs/cloud-messaging/android/receive-messages e https://learn.microsoft.com/aspnet/core/signalr/java-client
