﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyBook.Models;

public class ApplicationUser:IdentityUser
{
  [Required]
  public  string Name { get; set; }
  [Required]
  public  string SurName { get; set; }

  public string? StreetAddress { get; set; }
  public string? City { get; set; }
  public string? State { get; set; }
  public string? PostalCode { get; set; }
  
  public int? CompanyId { get; set; }
  [ForeignKey("CompanyId")]
  [ValidateNever]
  public Company Company { get; set; }

}