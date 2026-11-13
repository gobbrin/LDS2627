package pt.ipca.g15.campuscarpool
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color
import androidx.lifecycle.viewmodel.compose.viewModel
import pt.ipca.g15.campuscarpool.ui.AppNavigation
import pt.ipca.g15.campuscarpool.ui.AppViewModel
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val container = (application as CampusCarpoolApplication).container
        setContent {
            MaterialTheme(
                colorScheme = lightColorScheme(
                    primary = Color(0xFF173C33),
                    secondary = Color(0xFF627C39)
                )
            ) {
                val vm: AppViewModel = viewModel(factory = AppViewModel.Factory(container))
                AppNavigation(vm)
            }
        }
    }
}
