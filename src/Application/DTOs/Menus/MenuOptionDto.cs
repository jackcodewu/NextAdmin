using NextAdmin.Application.DTOs.Bases;

namespace NextAdmin.Application.DTOs.Menus
{
    public class MenuOptionDto : OptionDto
    {
        public List<MenuOptionDto> Children { get; set; }
    }

}
