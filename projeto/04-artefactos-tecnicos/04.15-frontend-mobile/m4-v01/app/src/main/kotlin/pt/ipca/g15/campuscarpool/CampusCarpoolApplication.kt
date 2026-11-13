package pt.ipca.g15.campuscarpool
import android.app.Application
import pt.ipca.g15.campuscarpool.di.AppContainer
class CampusCarpoolApplication : Application() {
    val container: AppContainer by lazy { AppContainer(this) }
}
