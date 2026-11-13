package pt.ipca.g15.campuscarpool.data.session

import com.google.gson.JsonParser
import java.util.Base64
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.domain.model.Sessao
import pt.ipca.g15.campuscarpool.domain.model.TokenAcesso

class SessaoManager(
    private val store: SessionStore,
    private val backend: String,
    private val scope: CoroutineScope,
    private val agora: () -> Long = System::currentTimeMillis
) {
    private val atual = MutableStateFlow<Sessao?>(null)
    val estado = atual.asStateFlow()
    private var expiracao: Job? = null
    init {
        val guardada = runCatching { store.ler() }.getOrNull()
        if (guardada != null &&
            runCatching {
                val (id, exp) = claims(guardada.accessToken)
                guardada.backend == backend &&
                    guardada.utilizadorId == id &&
                    guardada.expiraEm <= exp &&
                    guardada.expiraEm > agora()
            }.getOrDefault(false)
        ) {
            definir(guardada)
        } else {
            store.limpar()
        }
    }

    @Synchronized fun iniciar(token: TokenAcesso) {
        require(token.expiresIn in 1..86400) { "Validade de sessão inválida." }
        val (id, exp) = claims(token.accessToken)
        val sessao =
            Sessao(token.accessToken, id, minOf(exp, agora() + token.expiresIn * 1000), backend)
        require(sessao.expiraEm > agora()) { "Sessão expirada." }
        store.guardar(sessao)
        definir(sessao)
    }

    @Synchronized fun obterToken(): String? {
        if (atual.value?.expiraEm?.let { it <= agora() } == true) terminar()
        return atual.value?.accessToken
    }

    @Synchronized fun terminar() {
        expiracao?.cancel()
        expiracao = null
        atual.value = null
        store.limpar()
    }

    @Synchronized fun terminarSeToken(token: String?) {
        if (token != null &&
            atual.value?.accessToken == token
        ) {
            terminar()
        }
    }
    private fun definir(sessao: Sessao) {
        expiracao?.cancel()
        atual.value = sessao
        expiracao =
            scope.launch {
                delay((sessao.expiraEm - agora()).coerceAtLeast(1))
                terminarSeToken(sessao.accessToken)
            }
    }
    private fun claims(token: String): Pair<String, Long> {
        require(token.split('.').size == 3)
        val payload = JsonParser.parseString(
            String(Base64.getUrlDecoder().decode(token.split('.')[1]), Charsets.UTF_8)
        ).asJsonObject
        val id = payload.get("sub").asString
        val exp = Math.multiplyExact(payload.get("exp").asLong, 1000)
        require(id.isNotBlank())
        return id to exp
    }
}
