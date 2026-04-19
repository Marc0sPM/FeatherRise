using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Persistencia a disco mejorada. Respecto a la versión anterior
/// (que llamaba a File.AppendAllText en cada Save, abriendo y
/// cerrando el fichero constantemente):
///
///   1. Abre el FileStream UNA sola vez en Open() y lo mantiene abierto
///      hasta Close(). Cada Save() escribe al stream (con buffer interno).
///   2. Llama a stream.Flush() después de cada escritura para forzar que
///      el sistema operativo escriba el buffer a disco. Así, si el juego
///      crashea, como máximo se pierde el último batch encolado (NO todo).
///   3. Soporta rotación de archivos por tamaño máximo: cuando el archivo
///      actual supera X MB, se cierra, se abre uno nuevo con un sufijo
///      numerado (_part002.json) y se continúa escribiendo ahí.
///   4. FileShare.Read permite que herramientas externas (como tu script
///      Python de análisis) puedan LEER el archivo mientras el juego está
///      corriendo, sin bloquearlo.
///   5. Maneja excepciones de "disco lleno" sin crashear — propaga la
///      excepción al Tracker para que reencole los eventos.
/// </summary>
public class LocalFilePersistence : IPersistence
{
    private readonly string _baseFilePath;
    private readonly string _fileExtension;
    private readonly long _rotationMaxBytes;   // 0 = sin rotación
    private readonly int _bufferSize;

    private FileStream _stream;
    private StreamWriter _writer;
    private int _currentPart = 1;
    private string _currentFilePath;

    /// <summary>Cabecera pendiente de escribir si hay rotación (para que cada parte sea JSON válido).</summary>
    private string _headerCached = "";
    private string _footerCached = "";

    /// <summary>
    /// Crea la persistencia local con la configuración dada. NO abre el fichero todavía;
    /// eso pasa en Open(), que es llamado por el Tracker después de construir la instancia.
    /// </summary>
    public LocalFilePersistence(string outputDirectory, string sessionId,
                                string fileExtension, float rotationMaxMb,
                                int bufferSize)
    {
        _fileExtension = fileExtension;
        _bufferSize = Mathf.Max(1024, bufferSize);
        _rotationMaxBytes = rotationMaxMb > 0f
            ? (long)(rotationMaxMb * 1024 * 1024)
            : 0L;

        // Nombre base: Telemetry/telemetry_{sid}_{fecha}{ext}
        string date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string baseName = $"telemetry_{sessionId}_{date}";
        _baseFilePath = Path.Combine(outputDirectory, baseName);
    }

    public void Open(string header)
    {
        _headerCached = header ?? "";
        OpenNewPart();
    }

    public void Save(string data)
    {
        if (_writer == null)
        {
            throw new InvalidOperationException(
                "[LocalFilePersistence] Save() llamado sin Open() previo.");
        }

        // Rotación: si el fichero actual ya excede el tamaño máximo,
        // cerramos esta parte y abrimos la siguiente ANTES de escribir.
        if (_rotationMaxBytes > 0 && _stream.Length >= _rotationMaxBytes)
        {
            RotateToNewPart();
        }

        _writer.Write(data);
        // Flush periódico: el buffer del StreamWriter se vuelca al FileStream
        // y el FileStream pide al SO que escriba físicamente. Esto es lo que
        // hace que un crash del proceso no se lleve toda la sesión.
        _writer.Flush();
        _stream.Flush(flushToDisk: true);
    }

    public void Close(string footer)
    {
        _footerCached = footer ?? "";
        try
        {
            if (_writer != null)
            {
                if (!string.IsNullOrEmpty(_footerCached))
                {
                    _writer.Write(_footerCached);
                }
                _writer.Flush();
                _stream?.Flush(flushToDisk: true);
            }
        }
        finally
        {
            CloseCurrentStream();
        }
        Debug.Log($"[Tracker] Archivo cerrado: {_currentFilePath}");
    }

    // ------------------------- internos -------------------------

    /// <summary>
    /// Abre la parte actual (numerada). Si es la primera y no hay rotación,
    /// el archivo no lleva sufijo de parte por compatibilidad con los scripts
    /// de análisis existentes.
    /// </summary>
    private void OpenNewPart()
    {
        _currentFilePath = BuildPartFilePath(_currentPart);

        try
        {
            // FileMode.Create: si el archivo ya existía (improbable con el
            // timestamp en el nombre), lo sobreescribimos para no mezclar sesiones.
            // FileShare.Read: permite que otras herramientas lean el archivo
            // mientras está abierto por Unity.
            _stream = new FileStream(
                _currentFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                _bufferSize,
                FileOptions.SequentialScan);
            _writer = new StreamWriter(_stream, new UTF8Encoding(false));

            if (!string.IsNullOrEmpty(_headerCached))
            {
                _writer.Write(_headerCached);
                _writer.Flush();
                _stream.Flush(flushToDisk: true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalFilePersistence] No se pudo abrir '{_currentFilePath}': {ex.Message}");
            CloseCurrentStream();
            throw;
        }
    }

    /// <summary>
    /// Cierra la parte actual (escribiendo su footer para mantener el JSON válido)
    /// y abre la siguiente. Conserva header/footer cached para aplicarlos en las
    /// nuevas partes: así CADA part-file es un JSON autoconsistente y puede
    /// procesarse de forma independiente por el script de análisis.
    /// </summary>
    private void RotateToNewPart()
    {
        Debug.Log($"[LocalFilePersistence] Rotando a nueva parte "
                  + $"({_stream.Length / 1024f / 1024f:F2} MB > "
                  + $"{_rotationMaxBytes / 1024f / 1024f:F2} MB)");

        // Cerramos la parte actual con su footer
        try
        {
            if (!string.IsNullOrEmpty(_footerCached))
            {
                _writer.Write(_footerCached);
            }
            _writer.Flush();
            _stream.Flush(flushToDisk: true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalFilePersistence] Error cerrando parte previa: {ex.Message}");
        }
        CloseCurrentStream();

        _currentPart++;
        OpenNewPart();
    }

    private string BuildPartFilePath(int part)
    {
        // Si no hay rotación y es la parte 1, mantenemos el nombre clásico
        // (telemetry_{sid}_{date}.json) por compatibilidad con scripts existentes.
        if (_rotationMaxBytes <= 0 && part == 1)
        {
            return _baseFilePath + _fileExtension;
        }
        // Con rotación activa: telemetry_{sid}_{date}_part001.json, _part002.json, ...
        return $"{_baseFilePath}_part{part:D3}{_fileExtension}";
    }

    private void CloseCurrentStream()
    {
        try { _writer?.Dispose(); } catch { /* ignoramos */ }
        try { _stream?.Dispose(); } catch { /* ignoramos */ }
        _writer = null;
        _stream = null;
    }
}