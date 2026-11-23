package pt.ipca.g15.campuscarpool.ui.comunidade
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.google.gson.JsonObject
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.withContext
import pt.ipca.g15.campuscarpool.BuildConfig
import pt.ipca.g15.campuscarpool.CampusCarpoolApplication
object EventosPedido {
    val alteracoes = MutableSharedFlow<Unit>(extraBufferCapacity = 1)
}

@Composable fun LigacaoTempoReal() {
    val app = (LocalContext.current.applicationContext as CampusCarpoolApplication).container
    val sessaoAtual by app.sessao.estado.collectAsStateWithLifecycle()
    LaunchedEffect(sessaoAtual?.accessToken) {
        val conexao = HubConnectionBuilder.create(
            java.net.URI(BuildConfig.API_BASE_URL).resolve("/hubs/pedidos").toString()
        ).withAccessTokenProvider(
            Single.defer {
                Single.just(app.sessao.obterToken().orEmpty())
            }
        ).build()
        for (nome in listOf(
            "PedidoRecebido",
            "PedidoAceite",
            "PedidoRejeitado"
        )) {
            conexao.on(nome, { _: JsonObject ->
                EventosPedido.alteracoes.tryEmit(Unit)
            }, JsonObject::class.java)
        }
        try {
            while (isActive &&
                app.sessao.obterToken() != null
            ) {
                if (conexao.connectionState ==
                    HubConnectionState.DISCONNECTED
                ) {
                    try {
                        withContext(Dispatchers.IO) { conexao.start().blockingAwait() }
                        EventosPedido.alteracoes.tryEmit(Unit)
                    } catch (
                        e: CancellationException
                    ) {
                        throw e
                    } catch (
                        _: Exception
                    ) { /* O botão Atualizar mantém-se disponível. */ }
                }
                delay(10000)
            }
        } finally {
            withContext(
                NonCancellable + Dispatchers.IO
            ) { conexao.stop().blockingAwait() }
        }
    }
}
