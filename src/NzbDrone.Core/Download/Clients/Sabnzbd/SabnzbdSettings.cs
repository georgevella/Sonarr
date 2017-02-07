﻿using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public class SabnzbdSettingsValidator : AbstractValidator<SabnzbdSettings>
    {
        public SabnzbdSettingsValidator()
        {
            RuleFor(c => c.Host).ValidHost();
            RuleFor(c => c.Port).InclusiveBetween(1, 65535);

            RuleFor(c => c.ApiKey).NotEmpty()
                                  .WithMessage("API Key is required when username/password are not configured")
                                  .When(c => string.IsNullOrWhiteSpace(c.Username));

            RuleFor(c => c.Username).NotEmpty()
                                    .WithMessage("Username is required when API key is not configured")
                                    .When(c => string.IsNullOrWhiteSpace(c.ApiKey));

            RuleFor(c => c.Password).NotEmpty()
                                    .WithMessage("Password is required when API key is not configured")
                                    .When(c => string.IsNullOrWhiteSpace(c.ApiKey));

            RuleFor(c => c.TvCategory).NotEmpty()
                                      .WithMessage("A category for TV episodes is required.")
                                      .When(c => string.IsNullOrWhiteSpace(c.TvCategory));

            RuleFor(c => c.MovieCategory).NotEmpty()
                                      .WithMessage("A category for movies is required.")
                                      .When(c => string.IsNullOrWhiteSpace(c.MovieCategory));
        }
    }

    public class SabnzbdSettings : IProviderConfig, IDownloadClientSupportsCategories
    {
        private static readonly SabnzbdSettingsValidator Validator = new SabnzbdSettingsValidator();

        public SabnzbdSettings()
        {
            Host = "localhost";
            Port = 8080;
            TvCategory = "tv";
            MovieCategory = "movie";
            RecentTvPriority = (int)SabnzbdPriority.Default;
            OlderTvPriority = (int)SabnzbdPriority.Default;
        }

        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox)]
        public string Host { get; set; }

        [FieldDefinition(1, Label = "Port", Type = FieldType.Textbox)]
        public int Port { get; set; }

        [FieldDefinition(2, Label = "API Key", Type = FieldType.Textbox)]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "Username", Type = FieldType.Textbox)]
        public string Username { get; set; }

        [FieldDefinition(4, Label = "Password", Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(5, Label = "TV Shows Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Radarr avoids conflicts with unrelated downloads.")]
        public string TvCategory { get; set; }

        [FieldDefinition(5, Label = "Movies Category", Type = FieldType.Textbox, HelpText = "Adding a category specific to Radarr avoids conflicts with unrelated downloads.")]
        public string MovieCategory { get; set; }

        [FieldDefinition(6, Label = "Recent Priority", Type = FieldType.Select, SelectOptions = typeof(SabnzbdPriority), HelpText = "Priority to use when grabbing episodes that aired within the last 14 days")]
        public int RecentTvPriority { get; set; }

        [FieldDefinition(7, Label = "Older Priority", Type = FieldType.Select, SelectOptions = typeof(SabnzbdPriority), HelpText = "Priority to use when grabbing episodes that aired over 14 days ago")]
        public int OlderTvPriority { get; set; }

        [FieldDefinition(8, Label = "Use SSL", Type = FieldType.Checkbox)]
        public bool UseSsl { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
