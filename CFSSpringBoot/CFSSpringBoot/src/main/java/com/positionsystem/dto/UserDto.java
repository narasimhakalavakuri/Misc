// package com.positionsystem.dto;

// import lombok.AllArgsConstructor;
// import lombok.Data;
// import lombok.NoArgsConstructor;

// @Data
// @NoArgsConstructor
// @AllArgsConstructor
// public class UserDto {
//     private Long id;
//     private String userid;
//     private String department;
//     private String accessMask;
//     private String fullName;
//     private String email;
// }
package com.positionsystem.dto;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class UserDto {
    private Long id;
    private String userid;
    private String department;
    private String accessMask;
    private String fullName;
    private String email;
}
