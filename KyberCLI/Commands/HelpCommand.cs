namespace KyberCLI.Commands;

public sealed class HelpCommand : IKyberCommand
{
    public Task<int> ExecuteAsync()
    {
        Console.WriteLine("KyberCLI - client natif pour KyberDaemon\n");
        Console.WriteLine("Commandes disponibles:");
        Console.WriteLine("  connect --host <hôte> --port <port> --user <utilisateur> [options]");
        Console.WriteLine("  keys <list|rotate|export> [options]");
        Console.WriteLine("  passhash --username <utilisateur> --password <mot_de_passe>");
        Console.WriteLine();
        Console.WriteLine("Options de 'connect':");
        Console.WriteLine("  --password <secret>           Mot de passe texte (nécessite --password-record)");
        Console.WriteLine("  --password-file <fichier>     Lit le mot de passe texte depuis un fichier");
        Console.WriteLine("  --password-record <record>    Enregistrement Argon2id (chaine ou fichier)");
        Console.WriteLine("  --password-record-file <fichier> Charger l'enregistrement Argon2id depuis un fichier");
        Console.WriteLine("  --password-hex <hex>          Secret hexadécimal pré-dérivé (compatibilité)");
        Console.WriteLine("  --key <fichier>               Clé privée Dilithium (base64) pour signature");
        Console.WriteLine("  --command <cmd>               Commande unique à exécuter (sinon mode interactif)");
        Console.WriteLine("  --kerberos-token <base64>     Ticket Kerberos (AP-REQ) encodé en Base64");
        Console.WriteLine("  --kerberos-token-file <f>     Lit le ticket Kerberos depuis un fichier");
        Console.WriteLine("  --oauth-token <jwt>          Jeton OAuth/JWT à présenter au serveur");
        Console.WriteLine("  --oauth-token-file <f>       Lit le jeton OAuth depuis un fichier");
        Console.WriteLine("  --tls                            Active le canal TLS (reverse proxy)");
        Console.WriteLine("  --tls-server-name <nom_serveur>  Nom du serveur pour la validation TLS (SNI)");
        Console.WriteLine("  --tls-cert-fingerprint <empreinte> Empreinte SHA256 du certificat serveur pour pinning");
        Console.WriteLine("  --tls-skip-verify                Ignorer la validation du certificat TLS (DANGEREUX)");
        Console.WriteLine("  --trace-level <none|handshake|auth|full>  Afficher les traces détaillées du handshake/authentification");
        Console.WriteLine("  --help                           Affiche cette aide");
        Console.WriteLine();

        Console.WriteLine("Options de 'keys':");
        Console.WriteLine("  --config <fichier>          Chemin du kyberd.conf (défaut: config/kyberd.conf)");
        Console.WriteLine("  --name <nom>                Nom logique de la clé (défaut: session)");
        Console.WriteLine("  --passphrase <valeur>       Passphrase en clair (override de la configuration)");
        Console.WriteLine("  --mode <dpapi|passphrase>   Force le mode de chiffrement");
        Console.WriteLine("  --days <n>                  Durée de validité en jours pour la rotation");
        Console.WriteLine("  --output <fichier>          Emplacement du metadata-export (sous-commande export)");
        Console.WriteLine();

        Console.WriteLine("Options passhash :");
        Console.WriteLine("  --username <nom>              Prépare la ligne complète auth.conf");
        Console.WriteLine("  --password <secret>           Mot de passe à dériver");
        Console.WriteLine("  --password-file <fichier>     Lit le mot de passe depuis un fichier");
        Console.WriteLine("  --salt <hex|Base64>           Sel personnalisé (sinon généré aléatoirement)");
        Console.WriteLine("  --memory <KB>                 Mémoire Argon2 (defaut 65536)");
        Console.WriteLine("  --iterations <n>              Itérations Argon2 (defaut 3)");
        Console.WriteLine("  --parallelism <n>             Degré de parallélisme (defaut 2)");
        Console.WriteLine("  --length <octets>             Taille du hash dérivé (defaut 32)");
        return Task.FromResult(0);
    }
}
