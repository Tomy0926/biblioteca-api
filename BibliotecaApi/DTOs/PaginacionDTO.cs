namespace BibliotecaApi.DTOs
{
    public record PaginacionDTO(int Paginar = 1, int RecordsPorPagina = 10)
    {
        private const int CantidadMaxRecordsPorPagina = 50;

        public int Paginar { get; set; } = Math.Max(1, Paginar);
        public int RecordsPorPagina { get; set; } = Math.Clamp(RecordsPorPagina, 1, CantidadMaxRecordsPorPagina);
    }
}
