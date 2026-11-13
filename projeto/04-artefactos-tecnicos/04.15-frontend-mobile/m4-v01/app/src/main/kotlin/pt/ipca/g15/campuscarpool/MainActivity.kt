package pt.ipca.g15.campuscarpool
import android.content.Intent
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color
import androidx.lifecycle.ViewModelProvider
import pt.ipca.g15.campuscarpool.domain.model.LigacaoApp
import pt.ipca.g15.campuscarpool.ui.AppNavigation
import pt.ipca.g15.campuscarpool.ui.FabricaViewModels
import pt.ipca.g15.campuscarpool.ui.autenticacao.LoginViewModel
class MainActivity : ComponentActivity() {
    private lateinit var login: LoginViewModel

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val container = (application as CampusCarpoolApplication).container
        val fabrica = FabricaViewModels(container.sessao, container.autenticacao, container.perfil)
        login = ViewModelProvider(this, fabrica)[LoginViewModel::class.java]
        // Ao recriar a atividade, a ligação recebida já foi tratada.
        if (savedInstanceState == null) abrirLigacao(intent)
        setContent {
            MaterialTheme(
                colorScheme = lightColorScheme(
                    primary = Color(0xFF173C33),
                    secondary = Color(0xFF627C39)
                )
            ) {
                AppNavigation(login, container.conectividade.online, fabrica)
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        abrirLigacao(intent)
    }

    private fun abrirLigacao(intent: Intent) {
        LigacaoApp.interpretar(intent.dataString)?.let(login::abrirLigacao)
    }
}
