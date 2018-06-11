﻿using Data.Access.Repositories;
using LightInject;

namespace Data.Services
{
    public class CompositionRoot : ICompositionRoot
    {
        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.Register<IMarkRepository>(factory => new MarkRepository(), new PerRequestLifeTime());
            serviceRegistry.Register<IMarkService, MarkService>(new PerRequestLifeTime());
        }
    }
}