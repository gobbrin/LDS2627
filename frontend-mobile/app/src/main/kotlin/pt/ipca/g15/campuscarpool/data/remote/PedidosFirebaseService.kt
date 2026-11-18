package pt.ipca.g15.campuscarpool.data.remote
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import pt.ipca.g15.campuscarpool.CampusCarpoolApplication
import pt.ipca.g15.campuscarpool.MainActivity
class PedidosFirebaseService : FirebaseMessagingService() {
    override fun onNewToken(token: String) {
        (application as CampusCarpoolApplication).container.push.atualizar(token)
    }
    override fun onMessageReceived(message: RemoteMessage) {
        if (android.os.Build.VERSION.SDK_INT >= 33 &&
            checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS) !=
            android.content.pm.PackageManager.PERMISSION_GRANTED
        ) {
            return
        }
        val app = (application as CampusCarpoolApplication).container
        val tipo = message.data["tipo"]
        if (tipo !in
            setOf("PedidoRecebido", "PedidoAceite", "PedidoRejeitado") ||
            app.sessao.obterToken() == null ||
            !app.push.permitido()
        ) {
            return
        }
        val manager = getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(
            NotificationChannel(
                "pedidos",
                "Pedidos de participação",
                NotificationManager.IMPORTANCE_DEFAULT
            )
        )
        val intento = PendingIntent.getActivity(
            this,
            0,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )
        val n = android.app.Notification.Builder(
            this,
            "pedidos"
        ).setSmallIcon(
            android.R.drawable.ic_dialog_info
        ).setContentTitle(
            "Campus Carpool"
        ).setContentText(
            "Tens uma atualização de um pedido de participação."
        ).setContentIntent(intento).setAutoCancel(true).build()
        try {
            manager.notify(
                (
                    message.messageId
                        ?: tipo
                    ).hashCode(),
                n
            )
        } catch (
            _: SecurityException
        ) { /* Permissão revogada entre a verificação e a entrega. */ }
    }
}
