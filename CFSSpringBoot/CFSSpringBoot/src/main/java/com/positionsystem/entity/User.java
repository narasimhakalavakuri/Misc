
// package com.positionsystem.entity;

// import jakarta.persistence.*;
// import lombok.AllArgsConstructor;
// import lombok.Data;
// import lombok.NoArgsConstructor;
// import java.time.LocalDateTime;

// @Entity
// @Table(name = "tbl_user")
// @Data
// @NoArgsConstructor
// @AllArgsConstructor
// public class User {
    
//     @Id
//     @GeneratedValue(strategy = GenerationType.IDENTITY)
//     private Long id;
    
//     @Column(nullable = false, unique = true, length = 255)
//     private String userid;
    
//     @Column(nullable = false)
//     private String pwd;
    
//     @Column(nullable = false, length = 50)
//     private String department;
    
//     @Column(name = "access_mask", nullable = false, length = 50)
//     private String accessMask;
    
//     @Column(name = "full_name", length = 255)
//     private String fullName;
    
//     @Column(length = 255)
//     private String email;
    
//     @Column(name = "created_date", nullable = false, updatable = false)
//     private LocalDateTime createdDate;
    
//     @Column(name = "modified_date")
//     private LocalDateTime modifiedDate;
    
//     @PrePersist
//     protected void onCreate() {
//         createdDate = LocalDateTime.now();
//     }
    
//     @PreUpdate
//     protected void onUpdate() {
//         modifiedDate = LocalDateTime.now();
//     }
// }

package com.positionsystem.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.time.LocalDateTime;

@Entity
@Table(name = "tbl_user")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(nullable = false, unique = true, length = 255)
    private String userid;

    @Column(nullable = false)
    private String pwd;

    @Column(nullable = false, length = 50)
    private String department;

    @Column(name = "access_mask", nullable = false, length = 50)
    private String accessMask;

    @Column(name = "full_name", length = 255)
    private String fullName;

    @Column(length = 255)
    private String email;

    @Column(name = "created_date", nullable = false, updatable = false)
    private LocalDateTime createdDate;

    @Column(name = "modified_date")
    private LocalDateTime modifiedDate;

    @PrePersist
    protected void onCreate() {
        createdDate = LocalDateTime.now();
    }

    @PreUpdate
    protected void onUpdate() {
        modifiedDate = LocalDateTime.now();
    }
}