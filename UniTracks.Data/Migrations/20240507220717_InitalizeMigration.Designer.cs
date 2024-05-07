﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UniTracks.Data.SQLite;

#nullable disable

namespace UniTracks.Data.Migrations
{
    [DbContext(typeof(SqliteDBContext))]
    [Migration("20240507220717_InitalizeMigration")]
    partial class InitalizeMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("UniTracks.Models.Location.Location", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<double>("Accuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("Altitude")
                        .HasColumnType("REAL");

                    b.Property<double>("Heading")
                        .HasColumnType("REAL");

                    b.Property<double>("HeadingAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("Latitude")
                        .HasColumnType("REAL");

                    b.Property<double>("Longitude")
                        .HasColumnType("REAL");

                    b.Property<double>("Speed")
                        .HasColumnType("REAL");

                    b.Property<double>("SpeedAccuracy")
                        .HasColumnType("REAL");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Locations");
                });
#pragma warning restore 612, 618
        }
    }
}
