using Cassandra;
using FunkoApiV._2.Config;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using StackExchange.Redis;
using ISession = Cassandra.ISession;

namespace FunkoApiV._2.Infrastructure;

public static class ProductionDataCleaner {
    public static async Task ClearAsync(
        AppDbContext db,
        ISession cassandraSession,
        ILogger logger) {
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE funko, categoria, "AspNetUsers" RESTART IDENTITY CASCADE;""");

        var mongoClient = new MongoClient(FunkoConfig.MongoConnection);
        var proveedorCollection = mongoClient
            .GetDatabase(FunkoConfig.MongoDatabase)
            .GetCollection<Proveedor>("proveedor");
        await proveedorCollection.DeleteManyAsync(FilterDefinition<Proveedor>.Empty);

        await cassandraSession.ExecuteAsync(new SimpleStatement("TRUNCATE funkos_por_categorias"));
        await cassandraSession.ExecuteAsync(new SimpleStatement("TRUNCATE funkos_por_proveedor"));
        await cassandraSession.ExecuteAsync(new SimpleStatement("TRUNCATE funkos_por_proveedor_categoria"));

        using var redis = await ConnectionMultiplexer.ConnectAsync(FunkoConfig.RedisConnection);
        var redisDatabase = redis.GetDatabase();
        foreach (var endpoint in redis.GetEndPoints()) {
            var server = redis.GetServer(endpoint);
            if (!server.IsConnected)
                continue;

            foreach (var pattern in new[] { "funko:*", "categoria:*", "proveedor:*" }) {
                foreach (var key in server.Keys(redisDatabase.Database, pattern: pattern))
                    await redisDatabase.KeyDeleteAsync(key);
            }
        }

        logger.LogWarning(
            "Datos de producción eliminados de PostgreSQL, MongoDB, Cassandra y caché Redis. Se conservan las definiciones de roles.");
    }
}
