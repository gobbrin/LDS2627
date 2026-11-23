package pt.ipca.g15.campuscarpool.di
import android.content.Context
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import pt.ipca.g15.campuscarpool.BuildConfig
import pt.ipca.g15.campuscarpool.data.remote.ApiFactory
import pt.ipca.g15.campuscarpool.data.remote.ConnectivityMonitor
import pt.ipca.g15.campuscarpool.data.repository.AutenticacaoRepository
import pt.ipca.g15.campuscarpool.data.repository.PerfilRepository
import pt.ipca.g15.campuscarpool.data.repository.ViagemRepository
import pt.ipca.g15.campuscarpool.data.session.AndroidSessionStore
import pt.ipca.g15.campuscarpool.data.session.SessaoManager
class AppContainer(context: Context) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private val url = ApiFactory.validarUrl(BuildConfig.API_BASE_URL, BuildConfig.DEBUG)
    val sessao = SessaoManager(AndroidSessionStore(context), url.toString(), scope)
    private val api = ApiFactory.criar(url, sessao)
    val comunidadeApi = ApiFactory.criarServico(
        url,
        sessao,
        pt.ipca.g15.campuscarpool.data.remote.ComunidadeApi::class.java
    )
    val autenticacao =
        AutenticacaoRepository(api, sessao) {
            context.getSharedPreferences("push", Context.MODE_PRIVATE).getString("token", null)
        }
    val perfil = PerfilRepository(api)
    val viagens = ViagemRepository(api)
    val conectividade = ConnectivityMonitor(context)
    val push = pt.ipca.g15.campuscarpool.data.remote.PushGestor(context, this)
}
