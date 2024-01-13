﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

#pragma warning disable 219, 612, 618
#nullable disable

namespace calledudeBot.Database.Compiled
{
    public partial class DatabaseContextModel
    {
        partial void Initialize()
        {
            var userActivity = UserActivityEntityType.Create(this);
            var userSession = UserSessionEntityType.Create(this);

            UserActivityEntityType.CreateAnnotations(userActivity);
            UserSessionEntityType.CreateAnnotations(userSession);

            AddAnnotation("ProductVersion", "8.0.1");
            AddRuntimeAnnotation("Relational:RelationalModel", CreateRelationalModel());
        }

        private IRelationalModel CreateRelationalModel()
        {
            var relationalModel = new RelationalModel(this);

            var userActivity = FindEntityType("calledudeBot.Database.Activity.UserActivity")!;

            var defaultTableMappings = new List<TableMappingBase<ColumnMappingBase>>();
            userActivity.SetRuntimeAnnotation("Relational:DefaultMappings", defaultTableMappings);
            var calledudeBotDatabaseActivityUserActivityTableBase = new TableBase("calledudeBot.Database.Activity.UserActivity", null, relationalModel);
            var lastJoinDateColumnBase = new ColumnBase<ColumnMappingBase>("LastJoinDate", "TEXT", calledudeBotDatabaseActivityUserActivityTableBase);
            calledudeBotDatabaseActivityUserActivityTableBase.Columns.Add("LastJoinDate", lastJoinDateColumnBase);
            var messagesSentColumnBase = new ColumnBase<ColumnMappingBase>("MessagesSent", "INTEGER", calledudeBotDatabaseActivityUserActivityTableBase);
            calledudeBotDatabaseActivityUserActivityTableBase.Columns.Add("MessagesSent", messagesSentColumnBase);
            var streamSessionColumnBase = new ColumnBase<ColumnMappingBase>("StreamSession", "TEXT", calledudeBotDatabaseActivityUserActivityTableBase);
            calledudeBotDatabaseActivityUserActivityTableBase.Columns.Add("StreamSession", streamSessionColumnBase);
            var timesSeenColumnBase = new ColumnBase<ColumnMappingBase>("TimesSeen", "INTEGER", calledudeBotDatabaseActivityUserActivityTableBase);
            calledudeBotDatabaseActivityUserActivityTableBase.Columns.Add("TimesSeen", timesSeenColumnBase);
            var usernameColumnBase = new ColumnBase<ColumnMappingBase>("Username", "TEXT", calledudeBotDatabaseActivityUserActivityTableBase);
            calledudeBotDatabaseActivityUserActivityTableBase.Columns.Add("Username", usernameColumnBase);
            relationalModel.DefaultTables.Add("calledudeBot.Database.Activity.UserActivity", calledudeBotDatabaseActivityUserActivityTableBase);
            var calledudeBotDatabaseActivityUserActivityMappingBase = new TableMappingBase<ColumnMappingBase>(userActivity, calledudeBotDatabaseActivityUserActivityTableBase, true);
            calledudeBotDatabaseActivityUserActivityTableBase.AddTypeMapping(calledudeBotDatabaseActivityUserActivityMappingBase, false);
            defaultTableMappings.Add(calledudeBotDatabaseActivityUserActivityMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)usernameColumnBase, userActivity.FindProperty("Username")!, calledudeBotDatabaseActivityUserActivityMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)lastJoinDateColumnBase, userActivity.FindProperty("LastJoinDate")!, calledudeBotDatabaseActivityUserActivityMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)messagesSentColumnBase, userActivity.FindProperty("MessagesSent")!, calledudeBotDatabaseActivityUserActivityMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)streamSessionColumnBase, userActivity.FindProperty("StreamSession")!, calledudeBotDatabaseActivityUserActivityMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)timesSeenColumnBase, userActivity.FindProperty("TimesSeen")!, calledudeBotDatabaseActivityUserActivityMappingBase);

            var tableMappings = new List<TableMapping>();
            userActivity.SetRuntimeAnnotation("Relational:TableMappings", tableMappings);
            var userActivitiesTable = new Table("UserActivities", null, relationalModel);
            var usernameColumn = new Column("Username", "TEXT", userActivitiesTable);
            userActivitiesTable.Columns.Add("Username", usernameColumn);
            var lastJoinDateColumn = new Column("LastJoinDate", "TEXT", userActivitiesTable);
            userActivitiesTable.Columns.Add("LastJoinDate", lastJoinDateColumn);
            var messagesSentColumn = new Column("MessagesSent", "INTEGER", userActivitiesTable);
            userActivitiesTable.Columns.Add("MessagesSent", messagesSentColumn);
            var streamSessionColumn = new Column("StreamSession", "TEXT", userActivitiesTable);
            userActivitiesTable.Columns.Add("StreamSession", streamSessionColumn);
            var timesSeenColumn = new Column("TimesSeen", "INTEGER", userActivitiesTable);
            userActivitiesTable.Columns.Add("TimesSeen", timesSeenColumn);
            var pK_UserActivities = new UniqueConstraint("PK_UserActivities", userActivitiesTable, new[] { usernameColumn });
            userActivitiesTable.PrimaryKey = pK_UserActivities;
            var pK_UserActivitiesUc = RelationalModel.GetKey(this,
                "calledudeBot.Database.Activity.UserActivity",
                new[] { "Username" });
            pK_UserActivities.MappedKeys.Add(pK_UserActivitiesUc);
            RelationalModel.GetOrCreateUniqueConstraints(pK_UserActivitiesUc).Add(pK_UserActivities);
            userActivitiesTable.UniqueConstraints.Add("PK_UserActivities", pK_UserActivities);
            relationalModel.Tables.Add(("UserActivities", null), userActivitiesTable);
            var userActivitiesTableMapping = new TableMapping(userActivity, userActivitiesTable, true);
            userActivitiesTable.AddTypeMapping(userActivitiesTableMapping, false);
            tableMappings.Add(userActivitiesTableMapping);
            RelationalModel.CreateColumnMapping(usernameColumn, userActivity.FindProperty("Username")!, userActivitiesTableMapping);
            RelationalModel.CreateColumnMapping(lastJoinDateColumn, userActivity.FindProperty("LastJoinDate")!, userActivitiesTableMapping);
            RelationalModel.CreateColumnMapping(messagesSentColumn, userActivity.FindProperty("MessagesSent")!, userActivitiesTableMapping);
            RelationalModel.CreateColumnMapping(streamSessionColumn, userActivity.FindProperty("StreamSession")!, userActivitiesTableMapping);
            RelationalModel.CreateColumnMapping(timesSeenColumn, userActivity.FindProperty("TimesSeen")!, userActivitiesTableMapping);

            var userSession = FindEntityType("calledudeBot.Database.Session.UserSession")!;

            var defaultTableMappings0 = new List<TableMappingBase<ColumnMappingBase>>();
            userSession.SetRuntimeAnnotation("Relational:DefaultMappings", defaultTableMappings0);
            var calledudeBotDatabaseSessionUserSessionTableBase = new TableBase("calledudeBot.Database.Session.UserSession", null, relationalModel);
            var endTimeColumnBase = new ColumnBase<ColumnMappingBase>("EndTime", "TEXT", calledudeBotDatabaseSessionUserSessionTableBase);
            calledudeBotDatabaseSessionUserSessionTableBase.Columns.Add("EndTime", endTimeColumnBase);
            var idColumnBase = new ColumnBase<ColumnMappingBase>("Id", "INTEGER", calledudeBotDatabaseSessionUserSessionTableBase);
            calledudeBotDatabaseSessionUserSessionTableBase.Columns.Add("Id", idColumnBase);
            var startTimeColumnBase = new ColumnBase<ColumnMappingBase>("StartTime", "TEXT", calledudeBotDatabaseSessionUserSessionTableBase);
            calledudeBotDatabaseSessionUserSessionTableBase.Columns.Add("StartTime", startTimeColumnBase);
            var usernameColumnBase0 = new ColumnBase<ColumnMappingBase>("Username", "TEXT", calledudeBotDatabaseSessionUserSessionTableBase);
            calledudeBotDatabaseSessionUserSessionTableBase.Columns.Add("Username", usernameColumnBase0);
            var watchTimeColumnBase = new ColumnBase<ColumnMappingBase>("WatchTime", "TEXT", calledudeBotDatabaseSessionUserSessionTableBase);
            calledudeBotDatabaseSessionUserSessionTableBase.Columns.Add("WatchTime", watchTimeColumnBase);
            relationalModel.DefaultTables.Add("calledudeBot.Database.Session.UserSession", calledudeBotDatabaseSessionUserSessionTableBase);
            var calledudeBotDatabaseSessionUserSessionMappingBase = new TableMappingBase<ColumnMappingBase>(userSession, calledudeBotDatabaseSessionUserSessionTableBase, true);
            calledudeBotDatabaseSessionUserSessionTableBase.AddTypeMapping(calledudeBotDatabaseSessionUserSessionMappingBase, false);
            defaultTableMappings0.Add(calledudeBotDatabaseSessionUserSessionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)idColumnBase, userSession.FindProperty("Id")!, calledudeBotDatabaseSessionUserSessionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)endTimeColumnBase, userSession.FindProperty("EndTime")!, calledudeBotDatabaseSessionUserSessionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)startTimeColumnBase, userSession.FindProperty("StartTime")!, calledudeBotDatabaseSessionUserSessionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)usernameColumnBase0, userSession.FindProperty("Username")!, calledudeBotDatabaseSessionUserSessionMappingBase);
            RelationalModel.CreateColumnMapping((ColumnBase<ColumnMappingBase>)watchTimeColumnBase, userSession.FindProperty("WatchTime")!, calledudeBotDatabaseSessionUserSessionMappingBase);

            var tableMappings0 = new List<TableMapping>();
            userSession.SetRuntimeAnnotation("Relational:TableMappings", tableMappings0);
            var userSessionTable = new Table("UserSession", null, relationalModel);
            var idColumn = new Column("Id", "INTEGER", userSessionTable);
            userSessionTable.Columns.Add("Id", idColumn);
            var endTimeColumn = new Column("EndTime", "TEXT", userSessionTable);
            userSessionTable.Columns.Add("EndTime", endTimeColumn);
            var startTimeColumn = new Column("StartTime", "TEXT", userSessionTable);
            userSessionTable.Columns.Add("StartTime", startTimeColumn);
            var usernameColumn0 = new Column("Username", "TEXT", userSessionTable);
            userSessionTable.Columns.Add("Username", usernameColumn0);
            var watchTimeColumn = new Column("WatchTime", "TEXT", userSessionTable);
            userSessionTable.Columns.Add("WatchTime", watchTimeColumn);
            var pK_UserSession = new UniqueConstraint("PK_UserSession", userSessionTable, new[] { idColumn });
            userSessionTable.PrimaryKey = pK_UserSession;
            var pK_UserSessionUc = RelationalModel.GetKey(this,
                "calledudeBot.Database.Session.UserSession",
                new[] { "Id" });
            pK_UserSession.MappedKeys.Add(pK_UserSessionUc);
            RelationalModel.GetOrCreateUniqueConstraints(pK_UserSessionUc).Add(pK_UserSession);
            userSessionTable.UniqueConstraints.Add("PK_UserSession", pK_UserSession);
            relationalModel.Tables.Add(("UserSession", null), userSessionTable);
            var userSessionTableMapping = new TableMapping(userSession, userSessionTable, true);
            userSessionTable.AddTypeMapping(userSessionTableMapping, false);
            tableMappings0.Add(userSessionTableMapping);
            RelationalModel.CreateColumnMapping(idColumn, userSession.FindProperty("Id")!, userSessionTableMapping);
            RelationalModel.CreateColumnMapping(endTimeColumn, userSession.FindProperty("EndTime")!, userSessionTableMapping);
            RelationalModel.CreateColumnMapping(startTimeColumn, userSession.FindProperty("StartTime")!, userSessionTableMapping);
            RelationalModel.CreateColumnMapping(usernameColumn0, userSession.FindProperty("Username")!, userSessionTableMapping);
            RelationalModel.CreateColumnMapping(watchTimeColumn, userSession.FindProperty("WatchTime")!, userSessionTableMapping);
            return relationalModel.MakeReadOnly();
        }
    }
}
