// package com.positionsystem;

// import org.springframework.boot.SpringApplication;
// import org.springframework.boot.autoconfigure.SpringBootApplication;
// import org.springframework.boot.autoconfigure.EnableAutoConfiguration;
// // import org.springframework.boot.autoconfigure.security.servlet.SecurityAutoConfiguration;

// @EnableAutoConfiguration(exclude = {org.springframework.boot.autoconfigure.security.servlet.SecurityAutoConfiguration.class})
// @SpringBootApplication
// public class PositionReportingApplication {
//     public static void main(String[] args) {
//         SpringApplication.run(PositionReportingApplication.class, args);
//     }
// }
package com.positionsystem;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class PositionReportingApplication {
    public static void main(String[] args) {
        SpringApplication.run(PositionReportingApplication.class, args);
    }
}