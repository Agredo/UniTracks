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
    [Migration("20240509235330_AddWeatherHeartRateWeight")]
    partial class AddWeatherHeartRateWeight
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("UniTracks.Models.Environment.Weather", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<double>("CloudCover")
                        .HasColumnType("REAL");

                    b.Property<double>("Humidity")
                        .HasColumnType("REAL");

                    b.Property<Guid>("LocationID")
                        .HasColumnType("TEXT");

                    b.Property<double>("Pressure")
                        .HasColumnType("REAL");

                    b.Property<double>("Temperature")
                        .HasColumnType("REAL");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<double>("WindDirection")
                        .HasColumnType("REAL");

                    b.Property<double>("WindSpeed")
                        .HasColumnType("REAL");

                    b.HasKey("ID");

                    b.HasIndex("LocationID");

                    b.ToTable("Weathers");
                });

            modelBuilder.Entity("UniTracks.Models.Health.HeartRate", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<double>("Rate")
                        .HasColumnType("REAL");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("TripID")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("TripID");

                    b.ToTable("HeartRates");
                });

            modelBuilder.Entity("UniTracks.Models.Health.Weight", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("TripID")
                        .HasColumnType("TEXT");

                    b.Property<double>("WeightValue")
                        .HasColumnType("REAL");

                    b.HasKey("ID");

                    b.HasIndex("TripID");

                    b.ToTable("Weights");
                });

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

                    b.Property<Guid?>("TripID")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.HasIndex("TripID");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("UniTracks.Models.Trip.Trip", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<double>("AverageSpeed")
                        .HasColumnType("REAL");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Distance")
                        .HasColumnType("REAL");

                    b.Property<DateTimeOffset>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<double>("MaxAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MaxAltitude")
                        .HasColumnType("REAL");

                    b.Property<double>("MaxHeading")
                        .HasColumnType("REAL");

                    b.Property<double>("MaxHeadingAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MaxSpeed")
                        .HasColumnType("REAL");

                    b.Property<double>("MaxSpeedAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MinAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MinAltitude")
                        .HasColumnType("REAL");

                    b.Property<double>("MinHeading")
                        .HasColumnType("REAL");

                    b.Property<double>("MinHeadingAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MinSpeed")
                        .HasColumnType("REAL");

                    b.Property<double>("MinSpeedAccuracy")
                        .HasColumnType("REAL");

                    b.Property<double>("MovingTime")
                        .HasColumnType("REAL");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<double>("StoppedTime")
                        .HasColumnType("REAL");

                    b.Property<double>("TotalTime")
                        .HasColumnType("REAL");

                    b.HasKey("ID");

                    b.ToTable("Trips");
                });

            modelBuilder.Entity("UniTracks.Models.Environment.Weather", b =>
                {
                    b.HasOne("UniTracks.Models.Location.Location", "Location")
                        .WithMany()
                        .HasForeignKey("LocationID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Location");
                });

            modelBuilder.Entity("UniTracks.Models.Health.HeartRate", b =>
                {
                    b.HasOne("UniTracks.Models.Trip.Trip", null)
                        .WithMany("HeartRates")
                        .HasForeignKey("TripID");
                });

            modelBuilder.Entity("UniTracks.Models.Health.Weight", b =>
                {
                    b.HasOne("UniTracks.Models.Trip.Trip", null)
                        .WithMany("Weights")
                        .HasForeignKey("TripID");
                });

            modelBuilder.Entity("UniTracks.Models.Location.Location", b =>
                {
                    b.HasOne("UniTracks.Models.Trip.Trip", null)
                        .WithMany("Locations")
                        .HasForeignKey("TripID");
                });

            modelBuilder.Entity("UniTracks.Models.Trip.Trip", b =>
                {
                    b.Navigation("HeartRates");

                    b.Navigation("Locations");

                    b.Navigation("Weights");
                });
#pragma warning restore 612, 618
        }
    }
}
