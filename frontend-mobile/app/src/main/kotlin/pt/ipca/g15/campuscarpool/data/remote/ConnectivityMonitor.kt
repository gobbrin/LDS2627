package pt.ipca.g15.campuscarpool.data.remote
import android.content.Context
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
class ConnectivityMonitor(context: Context) {
    private val manager = context.getSystemService(ConnectivityManager::class.java)
    private val estado = MutableStateFlow(
        validada(manager.getNetworkCapabilities(manager.activeNetwork))
    )
    val online = estado.asStateFlow()
    private fun validada(caps: NetworkCapabilities?) =
        caps?.hasCapability(NetworkCapabilities.NET_CAPABILITY_VALIDATED) == true

    // Usa os dados recebidos: consultar o ConnectivityManager dentro do callback dá valores
    // desatualizados. O onCapabilitiesChanged chega sempre depois do onAvailable.
    private val callback = object : ConnectivityManager.NetworkCallback() {
        override fun onLost(network: Network) {
            estado.value = false
        }
        override fun onCapabilitiesChanged(network: Network, caps: NetworkCapabilities) {
            estado.value = validada(caps)
        }
    }
    init {
        manager.registerDefaultNetworkCallback(callback)
    }
}
