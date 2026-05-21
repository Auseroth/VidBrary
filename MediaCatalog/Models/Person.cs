namespace MediaCatalog.Models;

public class Person
{
    public int Id { get; set; }
    public int TmdbPersonId { get; set; }
    public required string Name { get; set; }
    public string? ProfilePath { get; set; }
    public string? Biography { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? PlaceOfBirth { get; set; }

    public ICollection<MovieCast> MovieCast { get; set; } = [];
    public ICollection<MovieCrew> MovieCrew { get; set; } = [];
    public ICollection<TvShowCast> TvShowCast { get; set; } = [];
}