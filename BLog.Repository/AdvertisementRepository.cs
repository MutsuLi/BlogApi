using Blog.IRepository;
using Blog.Model.Models;
using Blog.Repository.Base;
using Blog.IRepository.IUnitOfWork;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using SqlSugar;

namespace Blog.Repository
{
    public class AdvertisementRepository : BaseRepository<Advertisement>, IAdvertisementRepository
    {

        private DbContext context;
        private SqlSugarClient db;
        private SimpleClient<Advertisement> entityDB;
        internal SqlSugarClient Db
        {
            get { return db; }
            private set { db = value; }
        }
        public DbContext Context
        {
            get { return context; }
            set { context = value; }
        }
        public AdvertisementRepository()
        {
            DbContext.Init(BaseDBConfig.ConnectionString,DbType.MySql);
            context = DbContext.GetDbContext();
            db = context.Db;
            entityDB = context.GetEntityDB<Advertisement>(db);
        }
        //public AdvertisementRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        //{


        //}

        public int Add(Advertisement model)
        {
            //���ص�i��long����,��������Ը������ҵ����Ҫ���д���
            var i = db.Insertable(model).ExecuteReturnBigIdentity();
            return i.ObjToInt();
        }

        public bool Delete(Advertisement model)
        {
            var i = db.Deleteable(model).ExecuteCommand();
            return i > 0;
        }

        public List<Advertisement> Query(Expression<Func<Advertisement, bool>> whereExpression)
        {
            return entityDB.GetList(whereExpression);

        }

        public int Sum(int i, int j)
        {
            return i + j;
        }

        public bool Update(Advertisement model)
        {
            //���ַ�ʽ��������Ϊ����
            var i = db.Updateable(model).ExecuteCommand();
            return i > 0;
        }
    }
}
