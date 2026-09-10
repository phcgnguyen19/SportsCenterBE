using Microsoft.EntityFrameworkCore;

namespace SportsCenterAPI.Helpers
{
    /// <summary>
    /// Generic container for paginated result sets with pagination metadata.
    /// Lớp generic chứa dữ liệu của trang hiện tại cùng các siêu dữ liệu (metadata) phân trang.
    /// </summary>
    /// <typeparam name="T">Type of data items / Kiểu dữ liệu của các phần tử trong danh sách</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Collection of items on the current page / Danh sách dữ liệu của trang hiện tại
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Total number of records matching the query / Tổng số bản ghi thỏa mãn điều kiện lọc
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page index (1-based) / Số thứ tự trang hiện tại (bắt đầu từ 1)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page / Số lượng bản ghi trên mỗi trang
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of pages / Tổng số trang
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates whether a previous page exists / Cho biết có trang trước đó hay không
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indicates whether a next page exists / Cho biết có trang kế tiếp hay không
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Parameterless constructor for serialization / Hàm tạo không tham số phục vụ tuần tự hóa JSON
        /// </summary>
        public PagedResult()
        {
        }

        /// <summary>
        /// Constructor initializing pagination metadata and item list.
        /// Hàm tạo khởi tạo dữ liệu và tính toán tổng số trang.
        /// </summary>
        public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            PageSize = pageSize > 0 ? pageSize : 10;
            PageNumber = pageNumber > 0 ? pageNumber : 1;
            TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        }
    }

    /// <summary>
    /// Extension methods providing asynchronous pagination for IQueryable sources.
    /// Các phương thức mở rộng hỗ trợ phân trang bất đồng bộ cho IQueryable.
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Asynchronously paginates an IQueryable source based on page number and page size.
        /// Phân trang bất đồng bộ nguồn truy vấn IQueryable theo số trang và kích thước trang.
        /// </summary>
        /// <typeparam name="T">Entity or model type / Kiểu thực thể</typeparam>
        /// <param name="query">IQueryable query source / Nguồn truy vấn</param>
        /// <param name="pageNumber">Page number (1-based index) / Số thứ tự trang (bắt đầu từ 1)</param>
        /// <param name="pageSize">Number of records per page / Số lượng bản ghi mỗi trang</param>
        /// <returns>PagedResult containing items and pagination metadata / Đối tượng PagedResult chứa danh sách và metadata</returns>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query), "Query source cannot be null.");
            }

            // Normalize pageNumber and pageSize to safe defaults
            // Chuẩn hóa tham số: đảm bảo pageNumber >= 1 và pageSize >= 1
            var page = pageNumber < 1 ? 1 : pageNumber;
            var size = pageSize < 1 ? 10 : pageSize;

            // Retrieve total record count asynchronously
            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện
            var totalCount = await query.CountAsync();

            // Fetch current page data using Skip and Take
            // Lấy dữ liệu trang hiện tại bằng Skip và Take
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, page, size);
        }
    }
}
