package pt.ipca.g15.campuscarpool.data.repository
import java.io.IOException
import kotlinx.coroutines.CancellationException
import pt.ipca.g15.campuscarpool.data.remote.ApiService
import pt.ipca.g15.campuscarpool.data.remote.ErrosApi
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
import pt.ipca.g15.campuscarpool.domain.model.ConfirmarPedido
import pt.ipca.g15.campuscarpool.domain.model.Credenciais
import pt.ipca.g15.campuscarpool.domain.model.MensagemResposta
import pt.ipca.g15.campuscarpool.domain.model.ReenviarConfirmacaoPedido
import pt.ipca.g15.campuscarpool.domain.model.RegistoPedido
import pt.ipca.g15.campuscarpool.domain.model.Resultado
import pt.ipca.g15.campuscarpool.domain.model.TerminarSessaoPedido

class AutenticacaoRepository(private val api: ApiService, private val sessao: SessaoManager) {
    suspend fun registar(nome: String, email: String, password: String): Resultado<String> =
        ErrosApi.executar {
            api.registar(RegistoPedido(nome.trim(), email.trim(), password))
        }.soMensagem()

    suspend fun confirmar(token: String): Resultado<Unit> =
        ErrosApi.executarSemCorpo { api.confirmar(ConfirmarPedido(token)) }

    suspend fun reenviarConfirmacao(email: String): Resultado<String> = ErrosApi.executar {
        api.reenviarConfirmacao(ReenviarConfirmacaoPedido(email.trim()))
    }.soMensagem()

    private fun Resultado<MensagemResposta>.soMensagem(): Resultado<String> = when (this) {
        is Resultado.Falha -> this
        is Resultado.Sucesso -> Resultado.Sucesso(valor.mensagem)
    }

    suspend fun login(email: String, password: String): Resultado<Unit> {
        if (email.isBlank() ||
            password.isBlank()
        ) {
            return Resultado.Falha("Introduz o email e a palavra-passe.")
        }
        return when (
            val resposta = ErrosApi.executar {
                api.login(Credenciais(email.trim(), password))
            }
        ) {
            is Resultado.Falha -> resposta
            is Resultado.Sucesso -> {
                try {
                    sessao.iniciar(resposta.valor)
                } catch (
                    _: Exception
                ) {
                    return Resultado.Falha(
                        "Não foi possível guardar a sessão em segurança. Tenta novamente."
                    )
                }
                when (val validacao = validar()) {
                    is Resultado.Sucesso -> Resultado.Sucesso(Unit)
                    is Resultado.Falha -> {
                        sessao.terminar()
                        validacao
                    }
                }
            }
        }
    }
    suspend fun validar(): Resultado<Unit> = when (
        val resposta = ErrosApi.executar {
            api.sessaoAtual()
        }
    ) {
        is Resultado.Falha -> resposta
        is Resultado.Sucesso -> if (resposta.valor.id.isNotBlank() &&
            resposta.valor.id == sessao.estado.value?.utilizadorId
        ) {
            Resultado.Sucesso(Unit)
        } else {
            sessao.terminar()
            Resultado.Falha("A sessão não foi validada. Inicia sessão novamente.", 401)
        }
    }
    suspend fun logout(tokenDispositivo: String? = null): Resultado<Unit> {
        val token = sessao.obterToken()
        try {
            val r = api.logout(TerminarSessaoPedido(tokenDispositivo))
            return if (r.isSuccessful ||
                r.code() == 401
            ) {
                Resultado.Sucesso(Unit)
            } else {
                Resultado.Falha(
                    "Sessão local terminada; o servidor não confirmou a revogação.",
                    r.code()
                )
            }
        } catch (e: CancellationException) {
            throw e
        } catch (
            _: IOException
        ) {
            return Resultado.Falha(
                "Sessão local terminada; sem ligação para confirmar a revogação no servidor."
            )
        } finally {
            sessao.terminarSeToken(token)
        }
    }
}
