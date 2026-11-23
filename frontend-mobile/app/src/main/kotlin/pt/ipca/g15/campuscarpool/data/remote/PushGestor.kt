package pt.ipca.g15.campuscarpool.data.remote
import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.content.edit
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import pt.ipca.g15.campuscarpool.di.AppContainer
import pt.ipca.g15.campuscarpool.ui.comunidade.corpo
class PushGestor(private val context: Context, private val app: AppContainer) {
    private val prefs = context.getSharedPreferences("push", Context.MODE_PRIVATE)
    private val scope = CoroutineScope(
        SupervisorJob() + Dispatchers.IO
    )
    init {
        scope.launch {
            app.sessao.estado.collectLatest {
                if (it ==
                    null
                ) {
                    eliminarToken()
                } else {
                    sincronizar()
                }
            }
        }
    }
    fun permitido() = prefs.getBoolean("opt_in", false) &&
        (
            Build.VERSION.SDK_INT < 33 ||
                context.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) ==
                PackageManager.PERMISSION_GRANTED
            )
    fun ativar() {
        prefs.edit { putBoolean("opt_in", true) }
        sincronizar()
    }
    fun desativar() {
        prefs.edit { putBoolean("opt_in", false) }
        eliminarToken()
    }
    fun atualizar(token: String) {
        prefs.edit { putString("token", token) }
        val id =
            app.sessao.estado.value?.utilizadorId ?: return
        if (!permitido())return
        scope.launch {
            if (app.sessao.estado.value?.utilizadorId ==
                id
            ) {
                ErrosApi.executarSemCorpo { app.comunidadeApi.dispositivo(corpo("token" to token)) }
            }
        }
    }
    private fun sincronizar() {
        if (!permitido() ||
            app.sessao.obterToken() == null ||
            FirebaseApp.initializeApp(context) == null
        ) {
            return
        }
        FirebaseMessaging.getInstance().token.addOnSuccessListener { atualizar(it) }
    }
    private fun eliminarToken() {
        prefs.edit { remove("token") }
        if (FirebaseApp.initializeApp(context) !=
            null
        ) {
            FirebaseMessaging.getInstance().deleteToken()
        }
    }
}
