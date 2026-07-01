namespace OneeProject.Database.Common
{
    public class Message<TEntity>
    {
        public string Status { get; set; } = "E";
        public string Text { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public TEntity Result { get; set; }
    }

    public class DataResponse<TEntity>
    {
        public TEntity Data { get; set; }
        public Payload Payload { get; set; }
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Last_page { get; set; }
        public int Items_per_page { get; set; }
        public int Total { get; set; }
    }

    public class Payload
    {
        public PaginationInfo Pagination { get; set; }
    }

    public class FEPaginationHelper<TEntity>(int itemsPerPage, int totalItems)
    {
        public int Items_per_page { get; set; } = itemsPerPage;
        public int TotalItems { get; set; } = totalItems;
        public PaginationInfo GetPaginationInfo(int currentPage)
        {
            double totalPages = (double)TotalItems / Items_per_page;
            int roundedUpPages = (int)Math.Ceiling(totalPages);
            int startPage = Math.Max(1, currentPage - 2);
            int endPage = Math.Min(startPage + 3, roundedUpPages);

            return new PaginationInfo
            {
                Page = currentPage,
                Last_page = roundedUpPages,
                Items_per_page = Items_per_page,
                Total = TotalItems
            };
        }
    }
}
