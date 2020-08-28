using Blog.IRepository;
using Blog.Api.Model.EntityFrameworkCore;
using Blog.Api.Models;
using Blog.Repository.Base;

namespace Blog.Repository
{
    public class PostsRespository : Repository<BlogArticle>, IPostsRespository
    {
        public PostsRespository(EfcoreDbContext context) : base(context)
        {

        }
    }
}
