using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using ToolTester.Application.Common.Models;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



namespace ToolTester.Application.Common.Mapping;

    public static class MappingExtensions
    {
        public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize)
            => PaginatedList<TDestination>.CreateAsync(queryable, pageNumber, pageSize);

        public static Task<PaginatedData<TDestination>> PaginatedDataAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize)
                => PaginatedData<TDestination>.CreateAsync(queryable, pageNumber, pageSize);
        public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable, IConfigurationProvider configuration)
                => queryable.ProjectTo<TDestination>(configuration).ToListAsync();

        public static void IgnoreSourceWhenDefault<TSource, TDestination>(this IMemberConfigurationExpression<TSource, TDestination, object> opt)
        {
            var destinationType = opt.DestinationMember.GetMemberType();
            object defaultValue = destinationType.GetTypeInfo().IsValueType ? Activator.CreateInstance(destinationType) : null;
            opt.Condition((src, dest, srcValue) => !Equals(srcValue, defaultValue));
        }

        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            if (memberInfo is MethodInfo)
                return ((MethodInfo)memberInfo).ReturnType;
            if (memberInfo is PropertyInfo)
                return ((PropertyInfo)memberInfo).PropertyType;
            if (memberInfo is FieldInfo)
                return ((FieldInfo)memberInfo).FieldType;
            return null;
        }
    }


