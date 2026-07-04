using System;
using System.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Wrapper sécurisé pour stocker des clés cryptographiques en mémoire
    /// Implémente IDisposable pour nettoyage automatique lors de la destruction
    /// Utilise le pattern Dispose pour garantir le zeroization même en cas d'exception
    /// </summary>
    public class SecureKeyWrapper : IDisposable
    {
        private byte[] _keyData;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Indique si la clé a été nettoyée (zeroized)
        /// </summary>
        public bool IsZeroized => _disposed || (_keyData != null && SecureKeyManager.IsZeroized(_keyData));

        /// <summary>
        /// Indique si l'objet a été disposé
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Crée un nouveau wrapper sécurisé pour une clé
        /// </summary>
        /// <param name="keyData">Données de la clé (sera copiées)</param>
        public SecureKeyWrapper(byte[] keyData)
        {
            if (keyData != null)
            {
                _keyData = SecureKeyManager.SecureCopy(keyData);
            }
        }

        /// <summary>
        /// Crée un wrapper vide avec une taille spécifiée
        /// </summary>
        /// <param name="size">Taille du buffer à allouer</param>
        public SecureKeyWrapper(int size)
        {
            if (size > 0)
            {
                _keyData = new byte[size];
            }
        }

        /// <summary>
        /// Obtient une copie sécurisée des données de la clé
        /// L'appelant est responsable de nettoyer cette copie
        /// </summary>
        /// <returns>Copie des données de la clé</returns>
        /// <exception cref="ObjectDisposedException">Si l'objet a été disposé</exception>
        public byte[] GetKey()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureKeyWrapper), "La clé a été nettoyée et ne peut plus être utilisée");

            lock (_lockObject)
            {
                if (_keyData == null)
                    return null;

                return SecureKeyManager.SecureCopy(_keyData);
            }
        }

        /// <summary>
        /// Obtient les données de la clé sans copie (pour usage interne uniquement)
        /// À utiliser avec précaution, ne pas exposer à l'extérieur
        /// </summary>
        /// <returns>Référence aux données (peut être null si disposé)</returns>
        internal byte[] GetKeyUnsafe()
        {
            if (_disposed)
                return null;

            return _keyData;
        }

        /// <summary>
        /// Met à jour les données de la clé
        /// </summary>
        /// <param name="keyData">Nouvelles données (seront copiées)</param>
        /// <exception cref="ObjectDisposedException">Si l'objet a été disposé</exception>
        public void SetKey(byte[] keyData)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureKeyWrapper), "La clé a été nettoyée et ne peut plus être utilisée");

            lock (_lockObject)
            {
                // Nettoyer l'ancienne clé
                SecureKeyManager.Zeroize(_keyData);

                // Copier la nouvelle clé
                if (keyData != null)
                {
                    _keyData = SecureKeyManager.SecureCopy(keyData);
                }
                else
                {
                    _keyData = null;
                }
            }
        }

        /// <summary>
        /// Nettoie immédiatement la clé (zeroization) sans disposer l'objet
        /// Utile quand on veut réutiliser le wrapper
        /// </summary>
        public void Zeroize()
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                SecureKeyManager.Zeroize(_keyData);
            }
        }

        /// <summary>
        /// Nettoie la clé avec plusieurs passes
        /// </summary>
        /// <param name="passes">Nombre de passes de nettoyage</param>
        public void ZeroizeMultiplePasses(int passes = 3)
        {
            if (_disposed)
                return;

            lock (_lockObject)
            {
                if (_keyData != null)
                {
                    SecureKeyManager.ZeroizeMultiplePasses(_keyData, passes);
                }
            }
        }

        /// <summary>
        /// Obtient la longueur de la clé
        /// </summary>
        public int Length => _disposed ? 0 : (_keyData?.Length ?? 0);

        /// <summary>
        /// Implémentation de IDisposable
        /// Nettoie de manière sécurisée les données de la clé
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Méthode de nettoyage protégée
        /// </summary>
        /// <param name="disposing">True si appelé depuis Dispose(), false depuis le finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    if (disposing)
                    {
                        // Nettoyage multiple passes pour sécurité renforcée
                        SecureKeyManager.ZeroizeMultiplePasses(_keyData, 3);
                    }
                    else
                    {
                        // Depuis le finalizer, nettoyage simple
                        SecureKeyManager.Zeroize(_keyData);
                    }

                    _keyData = null;
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// Finalizer pour garantir le nettoyage même si Dispose() n'est pas appelé
        /// </summary>
        ~SecureKeyWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Conversion implicite en byte[] pour faciliter l'utilisation
        /// Retourne une copie des données
        /// </summary>
        public static implicit operator byte[](SecureKeyWrapper wrapper)
        {
            return wrapper?.GetKey();
        }
    }

    /// <summary>
    /// Wrapper sécurisé pour une paire de clés (publique et privée)
    /// La clé privée est automatiquement nettoyée lors du Dispose
    /// </summary>
    public class SecureKeyPairWrapper : IDisposable
    {
        private SecureKeyWrapper _publicKey;
        private SecureKeyWrapper _privateKey;
        private bool _disposed = false;

        /// <summary>
        /// Crée un nouveau wrapper pour une paire de clés
        /// </summary>
        /// <param name="publicKey">Clé publique</param>
        /// <param name="privateKey">Clé privée (sera nettoyée lors du Dispose)</param>
        public SecureKeyPairWrapper(byte[] publicKey, byte[] privateKey)
        {
            _publicKey = publicKey != null ? new SecureKeyWrapper(publicKey) : null;
            _privateKey = privateKey != null ? new SecureKeyWrapper(privateKey) : null;
        }

        /// <summary>
        /// Obtient la clé publique (copie)
        /// </summary>
        public byte[] GetPublicKey()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureKeyPairWrapper));
            
            return _publicKey?.GetKey();
        }

        /// <summary>
        /// Obtient la clé privée (copie)
        /// </summary>
        public byte[] GetPrivateKey()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureKeyPairWrapper));
            
            return _privateKey?.GetKey();
        }

        /// <summary>
        /// Nettoie immédiatement la clé privée (zeroization)
        /// La clé publique reste disponible
        /// </summary>
        public void ZeroizePrivateKey()
        {
            if (!_disposed)
            {
                _privateKey?.ZeroizeMultiplePasses(3);
            }
        }

        /// <summary>
        /// Implémentation de IDisposable
        /// Nettoie la clé privée de manière sécurisée
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Nettoyer la clé privée avec passes multiples
                _privateKey?.ZeroizeMultiplePasses(3);
                SecureKeyManager.SecureDispose(ref _privateKey);
                
                // La clé publique peut être nettoyée aussi
                SecureKeyManager.SecureDispose(ref _publicKey);
                
                _disposed = true;
            }
        }
    }
}

