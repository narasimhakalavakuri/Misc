package com.positionsystem.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.time.LocalDate;

@Entity
@Table(name = "deptlist")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Department {
    
    @Id
    @Column(name = "deptcode", length = 50)
    private String code;
    
    @Column(name = "deptdesc", nullable = false, length = 255)
    private String description;
    
    @Column(name = "is_closed")
    private Boolean isClosed = false;
    
    @Column(name = "closed_date")
    private LocalDate closedDate;
}