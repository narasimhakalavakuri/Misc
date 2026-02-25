package com.positionsystem.entity;

import jakarta.persistence.*;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Entity
@Table(name = "currency")
@Data
@NoArgsConstructor
@AllArgsConstructor
public class Currency {
    
    @Id
    @Column(name = "curr_code", length = 3)
    private String code;
    
    @Column(name = "longname", nullable = false, length = 255)
    private String longName;
    
    @Column(name = "deciml")
    private Integer decimals;
}