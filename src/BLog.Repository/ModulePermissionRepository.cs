﻿using Blog.IRepository;
using Blog.IRepository.IUnitOfWork;
using Blog.Model.Models;
using Blog.Repository.Base;

namespace Blog.Repository
{
    public class ModulePermissionRepository : BaseRepository<ModulePermission>, IModulePermissionRepository
    {
        public ModulePermissionRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
