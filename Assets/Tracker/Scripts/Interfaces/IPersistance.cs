public interface IPersistence
{
    void Open(string header);
    /// <summary>
    /// Guarda los datos serializados en el destino deseado. 
    /// </summary>
    /// <param name="data">Los datos serializados a guardar</param>
    void Save(string data);
    
    /// <summary>
    /// Cierra conexiones o streams de datos si es necesario. 
    /// </summary>
    void Close(string footer);
}